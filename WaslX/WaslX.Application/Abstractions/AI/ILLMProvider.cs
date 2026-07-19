using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AI;

/// <summary>
/// Provider-agnostic text generation. The application depends only on this seam; the concrete
/// implementation (currently Groq) resolves the configured generation model id and maps the
/// model-agnostic <see cref="LlmRequest"/> onto the vendor's API. No business logic should
/// ever depend on a specific model or vendor.
/// </summary>
public interface ILLMProvider
{
    /// <summary>Generates a complete answer. Never throws — transport/API failures come back as a failed Result.</summary>
    Task<Result<LlmResult>> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the answer incrementally (SSE under the hood). Transport errors surface as a thrown
    /// exception on enumeration; callers that prefer a Result should use <see cref="GenerateAsync"/>.
    /// </summary>
    IAsyncEnumerable<LlmStreamChunk> StreamAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
