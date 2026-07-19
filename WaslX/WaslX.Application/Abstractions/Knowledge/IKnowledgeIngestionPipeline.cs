using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>
/// Orchestrates Extract → Normalize → Chunk → Embed → Index for one <see cref="Domain.Entities.KnowledgeDocument"/>.
/// Idempotent: unchanged chunks (by content hash) are never re-embedded, and re-running deterministically
/// overwrites the same Qdrant points. Runs inside a Hangfire job — never throws to the caller.
/// </summary>
public interface IKnowledgeIngestionPipeline
{
    Task<Result> RunAsync(int documentId, CancellationToken cancellationToken = default);
}
