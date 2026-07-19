using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.Ai;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Ai.Providers;

/// <summary>
/// Direct Groq chat-completions client (OpenAI-compatible) used as the active <see cref="ILLMProvider"/>.
/// Groq's own embeddings endpoint has zero usable models for this account (confirmed live), so
/// embeddings run through <see cref="HuggingFaceEmbeddingProvider"/> instead — this class only ever
/// touches chat. Model id is config-only (<see cref="AiModelOptions.GenerationModelId"/>), never hardcoded.
///
/// CONFIRMED LIVE (2026-07-18) against https://api.groq.com/openai/v1/chat/completions with
/// model_id "llama-3.3-70b-versatile": standard OpenAI chat-completions request/response shape.
/// </summary>
internal sealed class GroqLlmProvider : ILLMProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AiModelOptions _modelOptions;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqLlmProvider> _logger;

    public GroqLlmProvider(HttpClient http, IOptions<GroqOptions> options, IOptions<AiModelOptions> modelOptions, ILogger<GroqLlmProvider> logger)
    {
        _options = options.Value;
        _modelOptions = modelOptions.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/");
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<Result<LlmResult>> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessagePayload> { new("system", request.System) };
        messages.AddRange(request.Messages.Select(m => new ChatMessagePayload(m.Role, m.Content)));

        var payload = new ChatRequest(_modelOptions.GenerationModelId, messages, request.MaxTokens, request.Temperature);

        var send = await SendWithRetryAsync(() => BuildJson(_options.ChatPath, payload), cancellationToken);
        if (send.IsFailure)
            return Result.Failure<LlmResult>(send.Error);

        try
        {
            var (text, inputTokens, outputTokens) = ParseChatResponse(send.Value);
            if (string.IsNullOrEmpty(text))
                return Result.Failure<LlmResult>(AppErrors.AiGenerationFailed);
            return Result.Success(new LlmResult(text, _modelOptions.GenerationModelId, inputTokens, outputTokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Groq chat response");
            return Result.Failure<LlmResult>(AppErrors.AiGenerationFailed);
        }
    }

    /// <summary>Not pinned to Groq's SSE format yet; yields the full answer as one terminal chunk.</summary>
    public async IAsyncEnumerable<LlmStreamChunk> StreamAsync(
        LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await GenerateAsync(request, cancellationToken);
        yield return new LlmStreamChunk(result.IsSuccess ? result.Value.Text : string.Empty, IsFinal: true);
    }

    private static (string Text, int InputTokens, int OutputTokens) ParseChatResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var text = string.Empty;
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                text = content.GetString() ?? string.Empty;
        }

        var inputTokens = 0;
        var outputTokens = 0;
        if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var ptv)) inputTokens = ptv;
            if (usage.TryGetProperty("completion_tokens", out var ct) && ct.TryGetInt32(out var ctv)) outputTokens = ctv;
        }

        return (text, inputTokens, outputTokens);
    }

    private HttpRequestMessage BuildJson(string relativePath, object payload) =>
        new(HttpMethod.Post, relativePath) { Content = JsonContent.Create(payload, options: Json) };

    /// <summary>Sends with exponential backoff on 429/5xx — same retry seam as <c>HuggingFaceEmbeddingProvider</c>.</summary>
    private async Task<Result<string>> SendWithRetryAsync(Func<HttpRequestMessage> build, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var request = build();
                using var response = await _http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                    return Result.Success(body);

                var retriable = (int)response.StatusCode == 429 || (int)response.StatusCode >= 500;
                if (retriable && attempt < maxAttempts)
                {
                    await Task.Delay(Backoff(attempt), cancellationToken);
                    continue;
                }

                _logger.LogError("Groq chat failed ({Status}): {Body}", (int)response.StatusCode, Truncate(body));
                return Result.Failure<string>(AppErrors.AiGatewayError);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt < maxAttempts)
                {
                    await Task.Delay(Backoff(attempt), cancellationToken);
                    continue;
                }
                _logger.LogError(ex, "Groq chat threw");
                return Result.Failure<string>(AppErrors.AiGatewayError);
            }
        }
    }

    private static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1) + Random.Shared.Next(0, 100));

    private static string Truncate(string s) => s.Length > 500 ? s[..500] : s;

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessagePayload> Messages,
        [property: JsonPropertyName("max_tokens")] int? MaxTokens,
        [property: JsonPropertyName("temperature")] double? Temperature);

    private sealed record ChatMessagePayload(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);
}
