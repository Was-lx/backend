using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Rag;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Rag;

/// <summary>
/// Read side of RAG: embeds the query (cached — identical queries never re-embed), searches Qdrant
/// tenant-filtered, drops low-score hits, and reranks for diversity. No SQL is touched — everything
/// needed is in the Qdrant payload.
/// </summary>
internal sealed class KnowledgeRetriever(
    IEmbeddingProvider embeddings,
    IVectorStore vectorStore,
    IReranker reranker,
    IMemoryCache cache,
    IOptions<RagOptions> ragOptions,
    IOptions<AiModelOptions> aiModels,
    ILogger<KnowledgeRetriever> logger) : IKnowledgeRetriever
{
    private static readonly TimeSpan QueryEmbeddingCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Result<RetrievalResult>> RetrieveAsync(
        int tenantId, string query, int? topK = null, string? sourceType = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result.Success(new RetrievalResult([]));

        var rag = ragOptions.Value;
        var effectiveTopK = topK ?? rag.TopK;

        var vector = await GetOrEmbedQueryAsync(query, cancellationToken);
        if (vector.IsFailure)
            return Result.Failure<RetrievalResult>(vector.Error);

        // Over-fetch before rerank/threshold so diversity filtering has real candidates to choose from.
        var searchResult = await vectorStore.SearchAsync(
            new VectorQuery(tenantId, vector.Value, Math.Max(effectiveTopK * 3, effectiveTopK), sourceType),
            cancellationToken);
        if (searchResult.IsFailure)
            return Result.Failure<RetrievalResult>(searchResult.Error);

        var candidates = searchResult.Value
            .Where(hit => hit.Score >= rag.MinScore)
            .Select(hit => new RetrievedChunk(
                hit.Payload.ChunkId, hit.Payload.DocumentId, hit.Payload.SourceType,
                hit.Payload.Language, hit.Payload.Content, hit.Payload.Title, hit.Score))
            .ToList();

        var final = reranker.Rerank(candidates, effectiveTopK);
        return Result.Success(new RetrievalResult(final));
    }

    private async Task<Result<float[]>> GetOrEmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        var cacheKey = $"kb:query-embed:{aiModels.Value.EmbeddingModelId}:{ComputeHash(query)}";
        if (cache.TryGetValue(cacheKey, out float[]? cached) && cached is not null)
            return Result.Success(cached);

        var embedResult = await embeddings.EmbedBatchAsync([query], EmbeddingPurpose.Query, cancellationToken);
        if (embedResult.IsFailure)
        {
            logger.LogWarning("Query embedding failed: {Error}", embedResult.Error.Code);
            return Result.Failure<float[]>(embedResult.Error);
        }

        var vector = embedResult.Value.Vectors.FirstOrDefault();
        if (vector is null)
            return Result.Failure<float[]>(AppErrors.EmbeddingFailed);

        cache.Set(cacheKey, vector, QueryEmbeddingCacheTtl);
        return Result.Success(vector);
    }

    private static string ComputeHash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
