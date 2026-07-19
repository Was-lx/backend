using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AI;

/// <summary>
/// Provider-agnostic embedding generation. Batches inputs in a single call for cost/latency.
/// The implementation resolves the configured embedding model id; no model name is hardcoded.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Embeds a batch of texts and returns one vector per input, in the same order. Never throws —
    /// failures come back as a failed Result. Callers should skip already-embedded (unchanged) text
    /// upstream to avoid duplicate embeddings. <paramref name="purpose"/> selects document vs query
    /// encoding for asymmetric models (Cohere).
    /// </summary>
    Task<Result<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> inputs, EmbeddingPurpose purpose = EmbeddingPurpose.Document, CancellationToken cancellationToken = default);
}
