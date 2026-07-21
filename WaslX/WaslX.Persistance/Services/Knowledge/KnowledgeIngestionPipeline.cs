using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Application.Abstractions.Rag;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Settings;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services.Knowledge;

/// <summary>
/// Orchestrates Extract -> Normalize -> Chunk -> Embed -> Index for one <see cref="KnowledgeDocument"/>.
/// Idempotent by design: each chunk gets a deterministic Qdrant point id (hash of documentId+chunkIndex),
/// and a chunk is only re-embedded when its content hash actually changed — unchanged chunks are
/// skipped entirely (no duplicate embeddings, no wasted gateway calls). Enqueued directly as a
/// Hangfire job (no separate job wrapper needed — mirrors IWhatsAppWebhookProcessor).
/// </summary>
[AutomaticRetry(Attempts = 3)]
internal sealed class KnowledgeIngestionPipeline(
    ApplicationDbContext db,
    IEnumerable<IKnowledgeSource> sources,
    ITextNormalizer normalizer,
    ITextChunker chunker,
    IEmbeddingProvider embeddings,
    IVectorStore vectorStore,
    IOptions<AiModelOptions> aiModels,
    ILogger<KnowledgeIngestionPipeline> logger) : IKnowledgeIngestionPipeline
{
    public async Task<Result> RunAsync(int documentId, CancellationToken cancellationToken = default)
    {
        var document = await db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
            return Result.Failure(AppErrors.KnowledgeDocumentNotFound);

        document.Status = KnowledgeDocumentStatus.Processing;
        document.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var source = sources.FirstOrDefault(s => s.SourceType == document.SourceType);
            if (source is null)
                return await FailAsync(document, $"No knowledge source registered for {document.SourceType}", cancellationToken);

            var rawTexts = new List<string>();
            await foreach (var item in source.ExtractAsync(document, cancellationToken))
                if (!string.IsNullOrWhiteSpace(item.Text))
                    rawTexts.Add(item.Text);

            if (rawTexts.Count == 0)
                return await FailAsync(document, "No extractable text found", cancellationToken);

            var normalized = normalizer.Normalize(string.Join("\n\n", rawTexts));
            var chunks = chunker.Chunk(normalized.Text);
            if (chunks.Count == 0)
                return await FailAsync(document, "Text produced no chunks", cancellationToken);

            var existingChunks = await db.KnowledgeVectors
                .Where(v => v.DocumentId == documentId)
                .ToListAsync(cancellationToken);
            var existingByIndex = existingChunks.ToDictionary(v => v.ChunkIndex);

            // Only chunks that are new or whose content actually changed get (re-)embedded.
            var hashesByIndex = chunks.ToDictionary(c => c.Index, c => ComputeHash(c.Text));
            var toEmbed = chunks
                .Where(c => !existingByIndex.TryGetValue(c.Index, out var row) || row.ContentHash != hashesByIndex[c.Index])
                .ToList();

            IReadOnlyList<float[]> newVectors = [];
            if (toEmbed.Count > 0)
            {
                var embedResult = await embeddings.EmbedBatchAsync(
                    toEmbed.Select(c => c.Text).ToList(), EmbeddingPurpose.Document, cancellationToken);
                if (embedResult.IsFailure)
                    return await FailAsync(document, $"Embedding failed: {embedResult.Error.Code}", cancellationToken);
                newVectors = embedResult.Value.Vectors;
            }
            var vectorByChunkIndex = toEmbed
                .Select((c, i) => (c.Index, Vector: newVectors[i]))
                .ToDictionary(x => x.Index, x => x.Vector);

            var sourceId = document.SourceRefId ?? document.Id;
            var touchedRows = new List<KnowledgeVector>();

            foreach (var chunk in chunks)
            {
                existingByIndex.TryGetValue(chunk.Index, out var row);
                var hash = hashesByIndex[chunk.Index];
                var isNewOrChanged = row is null || row.ContentHash != hash;

                if (row is null)
                {
                    row = new KnowledgeVector
                    {
                        TenantId = document.TenantId,
                        SourceType = document.SourceType,
                        SourceId = sourceId,
                        DocumentId = documentId,
                        ChunkIndex = chunk.Index,
                        QdrantPointId = DeterministicPointId(documentId, chunk.Index)
                    };
                    db.KnowledgeVectors.Add(row);
                }

                if (isNewOrChanged)
                {
                    row.TextContent = chunk.Text;
                    row.ContentHash = hash;
                    row.TokenCount = chunk.TokenCount;
                    row.EmbeddingModel = aiModels.Value.EmbeddingModelId;
                    row.Status = KnowledgeDocumentStatus.Indexed;
                    touchedRows.Add(row);
                }
            }

            // Chunks that existed before but fell outside the new chunk count are stale — remove
            // both the SQL row and the Qdrant point so the two stores never drift.
            var newIndexes = chunks.Select(c => c.Index).ToHashSet();
            var staleRows = existingChunks.Where(r => !newIndexes.Contains(r.ChunkIndex)).ToList();
            foreach (var stale in staleRows)
                db.KnowledgeVectors.Remove(stale);

            // Persist first so new rows get their SQL id (used as the payload's ChunkId).
            await db.SaveChangesAsync(cancellationToken);

            if (touchedRows.Count > 0)
            {
                var points = touchedRows.Select(row => new VectorPoint(
                    row.QdrantPointId,
                    vectorByChunkIndex[row.ChunkIndex],
                    new VectorPayload(
                        document.TenantId, sourceId, row.Id, documentId,
                        document.SourceType.ToString(), document.Language.ToString(),
                        row.TextContent, document.Title, document.Version + 1))
                ).ToList();

                var upsert = await vectorStore.UpsertAsync(points, cancellationToken);
                if (upsert.IsFailure)
                    return await FailAsync(document, $"Vector upsert failed: {upsert.Error.Code}", cancellationToken);
            }

            if (staleRows.Count > 0)
                await vectorStore.DeleteAsync(staleRows.Select(r => r.QdrantPointId).ToList(), cancellationToken);

            if (touchedRows.Count > 0 || staleRows.Count > 0)
                document.Version += 1;
            document.Status = KnowledgeDocumentStatus.Indexed;
            document.ChunkCount = chunks.Count;
            document.ErrorMessage = null;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Indexed knowledge document {DocumentId} ({SourceType}): {ChunkCount} chunks, {Embedded} (re-)embedded, {Stale} removed",
                documentId, document.SourceType, chunks.Count, touchedRows.Count, staleRows.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Knowledge ingestion failed for document {DocumentId}", documentId);
            return await FailAsync(document, ex.Message, cancellationToken);
        }
    }

    private async Task<Result> FailAsync(KnowledgeDocument document, string error, CancellationToken cancellationToken)
    {
        document.Status = KnowledgeDocumentStatus.Failed;
        document.ErrorMessage = error.Length > 2000 ? error[..2000] : error;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Failure(AppErrors.KnowledgeIngestionFailed(error));
    }

    private static string ComputeHash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    /// <summary>Stable Guid derived from (documentId, chunkIndex) so re-ingesting the same chunk
    /// always maps to the same Qdrant point — upsert overwrites in place, nothing needs tracking.</summary>
    private static Guid DeterministicPointId(int documentId, int chunkIndex)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"waslx-chunk:{documentId}:{chunkIndex}"));
        return new Guid(bytes[..16]);
    }
}
