using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.AI;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.AI;

/// <summary>
/// OpenAI Chat Completions client. Typed <see cref="HttpClient"/>; never throws to callers —
/// missing configuration, transport, or API failures come back as failed <see cref="Result{T}"/>s.
/// </summary>
internal sealed class OpenAiChatCompletionClient : IChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatCompletionClient> _logger;

    public OpenAiChatCompletionClient(HttpClient http, IOptions<OpenAiOptions> options, ILogger<OpenAiChatCompletionClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/");
        if (!string.IsNullOrWhiteSpace(_options.Organization))
            _http.DefaultRequestHeaders.Add("OpenAI-Organization", _options.Organization);
    }

    public async Task<Result<string>> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        // Key required (by design): fail clearly rather than returning canned text.
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured; summarization is unavailable.");
            return Result.Failure<string>(AppErrors.OpenAiNotConfigured);
        }

        try
        {
            var payload = new
            {
                model = _options.SummaryModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI completion failed ({Status}): {Body}", (int)response.StatusCode, body);
                return Result.Failure<string>(AppErrors.SummaryGenerationFailed);
            }

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure<string>(AppErrors.SummaryGenerationFailed);

            return Result.Success(content.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI completion threw.");
            return Result.Failure<string>(AppErrors.SummaryGenerationFailed);
        }
    }
}
