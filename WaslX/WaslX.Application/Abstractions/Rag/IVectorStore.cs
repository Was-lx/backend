using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Rag;

/// <summary>
/// The denormalized payload stored alongside each vector so retrieval needs no SQL round-trip.
/// Content and Title are copies of the authoritative rows in SQL (kept in sync on edit/delete).
/// </summary>
public record VectorPayload(
    int TenantId,
    int KnowledgeSourceId,
    long ChunkId,
    int DocumentId,
    string SourceType,
    string Language,
    string Content,
    string? Title,
    int Version);

/// <summary>A point to upsert: a stable id, its embedding, and the payload.</summary>
public record VectorPoint(Guid Id, float[] Vector, VectorPayload Payload);

/// <summary>A tenant-scoped similarity query. TenantId is mandatory — it is the isolation boundary.</summary>
public record VectorQuery(int TenantId, float[] Vector, int TopK, string? SourceType = null);

/// <summary>A retrieval hit: the payload plus the similarity score.</summary>
public record VectorHit(VectorPayload Payload, double Score);

/// <summary>
/// Vector database seam (currently Qdrant). Every read is filtered by <c>TenantId</c> in the payload
/// filter — isolation is caller-enforced here exactly as it is across the rest of WaslX.
/// </summary>
public interface IVectorStore
{
    /// <summary>Creates the collection if it does not exist (idempotent). Called once at startup.</summary>
    Task<Result> EnsureCollectionAsync(CancellationToken cancellationToken = default);

    Task<Result> UpsertAsync(IReadOnlyList<VectorPoint> points, CancellationToken cancellationToken = default);

    /// <summary>ANN search, tenant-filtered. Returns payload + score with no SQL lookup.</summary>
    Task<Result<IReadOnlyList<VectorHit>>> SearchAsync(VectorQuery query, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(IReadOnlyList<Guid> pointIds, CancellationToken cancellationToken = default);

    /// <summary>Deletes every point belonging to a document (used on re-index / document delete).</summary>
    Task<Result> DeleteByDocumentAsync(int tenantId, int documentId, CancellationToken cancellationToken = default);
}
