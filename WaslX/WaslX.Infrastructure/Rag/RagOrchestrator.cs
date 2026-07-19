using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Abstractions.Rag;
using WaslX.Domain.Results;

namespace WaslX.Infrastructure.Rag;

/// <summary>
/// Ties retrieval and generation together: embed+search for grounding context, build the prompt,
/// call the LLM, return a cited answer with a confidence score. If nothing relevant is retrieved,
/// returns a canned "I don't know, let me connect you with a human" fallback instead of guessing —
/// no LLM call is made in that case. Never sends anything to the customer; that is M8/M9's job.
/// </summary>
internal sealed class RagOrchestrator(
    IKnowledgeRetriever retriever,
    IPromptBuilder promptBuilder,
    ILLMProvider llm,
    ILogger<RagOrchestrator> logger) : IRagOrchestrator
{
    public async Task<Result<RagAnswer>> AskAsync(
        int tenantId, string question, string? conversationSummary = null, CustomerContext? customer = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return Result.Failure<RagAnswer>(AppErrors.AiGenerationFailed);

        var language = DetectLanguage(question);

        var retrieval = await retriever.RetrieveAsync(tenantId, question, cancellationToken: cancellationToken);
        if (retrieval.IsFailure)
            return Result.Failure<RagAnswer>(retrieval.Error);

        if (retrieval.Value.Chunks.Count == 0)
        {
            logger.LogInformation("No grounding context found for tenant {TenantId} — returning fallback", tenantId);
            return Result.Success(Fallback(language));
        }

        var request = promptBuilder.Build(question, retrieval.Value.Chunks, conversationSummary, customer, language);
        var generation = await llm.GenerateAsync(request, cancellationToken);
        if (generation.IsFailure)
            return Result.Failure<RagAnswer>(generation.Error);

        var topScore = retrieval.Value.Chunks.Max(c => c.Score);
        var confidence = Math.Clamp(topScore, 0, 1);

        return Result.Success(new RagAnswer(generation.Value.Text, retrieval.Value.Chunks, confidence, UsedFallback: false, language));
    }

    private static RagAnswer Fallback(string language) => new(
        language == "Arabic"
            ? "معنديش معلومات كافية عن الموضوع ده دلوقتي — هوصّلك بحد من الفريق يقدر يساعدك."
            : "I don't have enough information about that yet — let me connect you with a team member who can help.",
        Citations: [],
        Confidence: 0,
        UsedFallback: true,
        language);

    /// <summary>Simple Arabic-script-ratio heuristic — good enough to pick a reply language, not a full detector.</summary>
    private static string DetectLanguage(string text)
    {
        var arabicCount = text.Count(ch => ch is >= '\u0600' and <= '\u06FF');
        return arabicCount > text.Length * 0.3 ? "Arabic" : "English";
    }
}
