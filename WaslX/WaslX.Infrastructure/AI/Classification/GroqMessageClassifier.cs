using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.AI;
using WaslX.Application.Features.Classification.Models;

namespace WaslX.Infrastructure.AI.Classification;

public sealed class GroqOptions
{
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public int RequestTimeoutSeconds { get; set; } = 30;
}

public sealed class GroqMessageClassifier(
    HttpClient httpClient,
    ILogger<GroqMessageClassifier> logger,
    RuleBasedMessageClassifier fallback) : IMessageClassifier
{
    public async Task<MessageClassificationResult> ClassifyAsync(
        MessageClassificationInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildPrompt(input);
            var requestBody = new { model = "llama-3.3-70b-versatile", messages = new[] { new { role = "user", content = prompt } }, temperature = 0.1 };

            var response = await httpClient.PostAsync("/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return ParseResult(content ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Groq API call failed, falling back to rule-based classifier");
            return await fallback.ClassifyAsync(input, cancellationToken);
        }
    }

    private static string BuildPrompt(MessageClassificationInput input)
    {
        return $@"Classify this customer message. Return JSON only.
Message: ""{input.MessageText}""
Recent context: {string.Join(" | ", input.RecentMessages)}
Fields: topic (general/support/complaint/pricing/account), language (arabic/english/mixed/unknown), sentiment (positive/neutral/negative/angry), priority (low/normal/high/urgent), escalate (true/false), reason (short explanation if escalate=true, else empty)";
    }

    private static MessageClassificationResult ParseResult(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            return new MessageClassificationResult
            {
                Topic = root.TryGetProperty("topic", out var t) ? t.GetString() ?? "general" : "general",
                Language = root.TryGetProperty("language", out var l) ? l.GetString() ?? "unknown" : "unknown",
                Sentiment = root.TryGetProperty("sentiment", out var s) ? s.GetString() ?? "neutral" : "neutral",
                Priority = root.TryGetProperty("priority", out var p) ? p.GetString() ?? "normal" : "normal",
                Escalate = root.TryGetProperty("escalate", out var e) && e.GetBoolean(),
                Reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? string.Empty : string.Empty
            };
        }
        catch
        {
            return new MessageClassificationResult();
        }
    }
}
