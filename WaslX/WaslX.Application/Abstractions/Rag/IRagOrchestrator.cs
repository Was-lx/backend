using WaslX.Application.Abstractions.Ai;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Rag;

/// <summary>Known context about the customer, injected into the prompt when available.</summary>
public record CustomerContext(string? Name, bool Vip, string? Tier);

/// <summary>A grounded, cited answer plus the confidence signal auto-reply (M9) will gate on.</summary>
public record RagAnswer(string Text, IReadOnlyList<RetrievedChunk> Citations, double Confidence, bool UsedFallback, string Language);

/// <summary>Assembles the grounded prompt for a question — no vendor/model-specific logic here.</summary>
public interface IPromptBuilder
{
    LlmRequest Build(string question, IReadOnlyList<RetrievedChunk> context, string? conversationSummary, CustomerContext? customer, string language);
}

/// <summary>
/// Retrieval + generation, composed: retrieve grounded context, build the prompt, call the LLM,
/// and return a cited answer with a confidence score. Never sends anything — that's M8/M9's job.
/// </summary>
public interface IRagOrchestrator
{
    Task<Result<RagAnswer>> AskAsync(
        int tenantId, string question, string? conversationSummary = null, CustomerContext? customer = null,
        CancellationToken cancellationToken = default);
}
