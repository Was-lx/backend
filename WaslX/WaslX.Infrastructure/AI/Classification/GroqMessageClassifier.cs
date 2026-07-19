using System.Text.Json;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Ai;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.Classification.Models;

namespace WaslX.Infrastructure.AI.Classification;

public sealed class GroqMessageClassifier(
    ILLMProvider llmProvider,
    ILogger<GroqMessageClassifier> logger,
    RuleBasedMessageClassifier fallback) : IMessageClassifier
{
    private const string SystemPrompt = "You classify WaslX customer-support messages. Return one compact JSON object only.";

    public async Task<MessageClassificationResult> ClassifyAsync(
        MessageClassificationInput input, CancellationToken cancellationToken = default)
    {
        var request = new LlmRequest(
            SystemPrompt,
            [new LlmMessage("user", BuildPrompt(input))],
            Temperature: 0,
            MaxTokens: 300);

        var generation = await llmProvider.GenerateAsync(request, cancellationToken);
        if (generation.IsFailure)
        {
            logger.LogWarning("Message classification LLM call failed: {ErrorCode}. Falling back to rules.", generation.Error.Code);
            return await fallback.ClassifyAsync(input, cancellationToken);
        }

        var parsed = TryParseResult(generation.Value.Text, $"groq:{generation.Value.ModelId}");
        if (parsed is not null)
            return parsed;

        logger.LogWarning("Message classification LLM returned invalid JSON. Falling back to rules.");
        return await fallback.ClassifyAsync(input, cancellationToken);
    }

    private static string BuildPrompt(MessageClassificationInput input)
    {
        var recentContext = input.RecentMessages.Count == 0
            ? "none"
            : string.Join(" | ", input.RecentMessages.Take(5));
        var memory = string.IsNullOrWhiteSpace(input.CustomerMemorySummary)
            ? "none"
            : input.CustomerMemorySummary;

        return $$"""
Classify this customer message for routing and escalation.

Message:
{{input.MessageText}}

Recent conversation context:
{{recentContext}}

Customer memory summary:
{{memory}}

Return JSON with exactly these fields:
- topic: one of general, support, complaint, pricing, account, technical, delivery, sales
- language: one of arabic, english, mixed, unknown
- sentiment: one of positive, neutral, negative, angry
- priority: one of low, normal, high, urgent
- escalate: boolean
- reason: short explanation when escalate is true, otherwise empty string
""";
    }

    private static MessageClassificationResult? TryParseResult(string content, string classifierVersion)
    {
        try
        {
            var json = ExtractJson(content);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new MessageClassificationResult
            {
                Topic = GetString(root, "topic", "general"),
                Language = GetString(root, "language", "unknown"),
                Sentiment = GetString(root, "sentiment", "neutral"),
                Priority = GetString(root, "priority", "normal"),
                Escalate = root.TryGetProperty("escalate", out var e) && e.ValueKind is JsonValueKind.True,
                Reason = GetString(root, "reason", string.Empty),
                ClassifierVersion = classifierVersion
            };
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end >= start ? trimmed[start..(end + 1)] : trimmed;
    }

    private static string GetString(JsonElement root, string propertyName, string fallbackValue) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallbackValue
            : fallbackValue;
}