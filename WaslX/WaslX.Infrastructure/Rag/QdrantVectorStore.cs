using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using WaslX.Application.Abstractions.Rag;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;
using static Qdrant.Client.Grpc.Conditions;

namespace WaslX.Infrastructure.Rag;

/// <summary>
/// Qdrant-backed <see cref="IVectorStore"/>. Every point carries the denormalized payload so
/// retrieval needs no SQL round-trip; every read is filtered by <c>TenantId</c> (isolation boundary).
/// Never throws — gRPC/transport failures come back as failed Results.
/// </summary>
internal sealed class QdrantVectorStore : IVectorStore
{
    // Payload keys (kept in one place so upsert and read never drift).
    private const string KeyTenant = "TenantId";
    private const string KeySource = "KnowledgeSourceId";
    private const string KeyChunk = "ChunkId";
    private const string KeyDocument = "DocumentId";
    private const string KeySourceType = "SourceType";
    private const string KeyLanguage = "Language";
    private const string KeyContent = "Content";
    private const string KeyTitle = "Title";
    private const string KeyVersion = "Version";

    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(QdrantClient client, IOptions<QdrantOptions> options, ILogger<QdrantVectorStore> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _client.CollectionExistsAsync(_options.Collection, cancellationToken))
                return Result.Success();

            await _client.CreateCollectionAsync(
                _options.Collection,
                new VectorParams { Size = (ulong)_options.VectorSize, Distance = ParseDistance(_options.Distance) },
                cancellationToken: cancellationToken);

            // Payload indexes for the two fields we filter on.
            await _client.CreatePayloadIndexAsync(_options.Collection, KeyTenant, PayloadSchemaType.Integer, cancellationToken: cancellationToken);
            await _client.CreatePayloadIndexAsync(_options.Collection, KeySourceType, PayloadSchemaType.Keyword, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Qdrant collection {Collection} (size {Size}, {Distance})",
                _options.Collection, _options.VectorSize, _options.Distance);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection {Collection}", _options.Collection);
            return Result.Failure(AppErrors.VectorStoreError);
        }
    }

    public async Task<Result> UpsertAsync(IReadOnlyList<VectorPoint> points, CancellationToken cancellationToken = default)
    {
        if (points.Count == 0)
            return Result.Success();

        try
        {
            var structs = points.Select(p =>
            {
                var ps = new PointStruct
                {
                    Id = p.Id,
                    Vectors = p.Vector
                };
                ps.Payload[KeyTenant] = p.Payload.TenantId;
                ps.Payload[KeySource] = p.Payload.KnowledgeSourceId;
                ps.Payload[KeyChunk] = p.Payload.ChunkId;
                ps.Payload[KeyDocument] = p.Payload.DocumentId;
                ps.Payload[KeySourceType] = p.Payload.SourceType;
                ps.Payload[KeyLanguage] = p.Payload.Language;
                ps.Payload[KeyContent] = p.Payload.Content;
                ps.Payload[KeyTitle] = p.Payload.Title ?? string.Empty;
                ps.Payload[KeyVersion] = p.Payload.Version;
                return ps;
            }).ToList();

            await _client.UpsertAsync(_options.Collection, structs, cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant upsert failed ({Count} points)", points.Count);
            return Result.Failure(AppErrors.VectorStoreError);
        }
    }

    public async Task<Result<IReadOnlyList<VectorHit>>> SearchAsync(VectorQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new Filter { Must = { Match(KeyTenant, query.TenantId) } };
            if (!string.IsNullOrWhiteSpace(query.SourceType))
                filter.Must.Add(MatchKeyword(KeySourceType, query.SourceType));

            var results = await _client.QueryAsync(
                _options.Collection,
                query: query.Vector,
                filter: filter,
                limit: (ulong)query.TopK,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            var hits = results.Select(r => new VectorHit(ReadPayload(r.Payload), r.Score)).ToList();
            return Result.Success<IReadOnlyList<VectorHit>>(hits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant search failed for tenant {TenantId}", query.TenantId);
            return Result.Failure<IReadOnlyList<VectorHit>>(AppErrors.VectorStoreError);
        }
    }

    public async Task<Result> DeleteAsync(IReadOnlyList<Guid> pointIds, CancellationToken cancellationToken = default)
    {
        if (pointIds.Count == 0)
            return Result.Success();

        try
        {
            await _client.DeleteAsync(_options.Collection, pointIds.ToList(), cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant delete failed ({Count} points)", pointIds.Count);
            return Result.Failure(AppErrors.VectorStoreError);
        }
    }

    public async Task<Result> DeleteByDocumentAsync(int tenantId, int documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new Filter { Must = { Match(KeyTenant, tenantId), Match(KeyDocument, documentId) } };
            await _client.DeleteAsync(_options.Collection, filter, cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant delete-by-document failed (tenant {TenantId}, doc {DocumentId})", tenantId, documentId);
            return Result.Failure(AppErrors.VectorStoreError);
        }
    }

    private static VectorPayload ReadPayload(IDictionary<string, Value> p) => new(
        TenantId: (int)GetInt(p, KeyTenant),
        KnowledgeSourceId: (int)GetInt(p, KeySource),
        ChunkId: GetInt(p, KeyChunk),
        DocumentId: (int)GetInt(p, KeyDocument),
        SourceType: GetStr(p, KeySourceType),
        Language: GetStr(p, KeyLanguage),
        Content: GetStr(p, KeyContent),
        Title: GetStr(p, KeyTitle) is { Length: > 0 } t ? t : null,
        Version: (int)GetInt(p, KeyVersion));

    private static long GetInt(IDictionary<string, Value> p, string key) =>
        p.TryGetValue(key, out var v) ? v.IntegerValue : 0;

    private static string GetStr(IDictionary<string, Value> p, string key) =>
        p.TryGetValue(key, out var v) ? v.StringValue : string.Empty;

    private static Distance ParseDistance(string distance) => distance.ToLowerInvariant() switch
    {
        "dot" => Distance.Dot,
        "euclid" => Distance.Euclid,
        "manhattan" => Distance.Manhattan,
        _ => Distance.Cosine
    };
}
