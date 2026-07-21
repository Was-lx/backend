using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Rag;

/// <summary>A retrieved chunk, read entirely from the Qdrant payload — no SQL lookup on the hot path.</summary>
public record RetrievedChunk(long ChunkId, int DocumentId, string SourceType, string Language, string Content, string? Title, double Score);

public record RetrievalResult(IReadOnlyList<RetrievedChunk> Chunks);

/// <summary>
/// Read side of the RAG pipeline: embeds the query, searches Qdrant filtered by TenantId (the
/// isolation boundary), drops anything below <c>RagOptions.MinScore</c>, and reranks for diversity.
/// </summary>
public interface IKnowledgeRetriever
{
    Task<Result<RetrievalResult>> RetrieveAsync(
        int tenantId, string query, int? topK = null, string? sourceType = null, CancellationToken cancellationToken = default);
}
