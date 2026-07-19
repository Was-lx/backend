using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.Ai;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Ai.Providers;

/// <summary>
/// Embedding provider backed by Hugging Face's Inference Providers feature-extraction pipeline —
/// the active <see cref="IEmbeddingProvider"/>. Model id is config-only
/// (<see cref="AiModelOptions.EmbeddingModelId"/>), currently "BAAI/bge-m3". BGE-M3 is a symmetric
/// model (no document/query distinction), so <c>purpose</c> is accepted for interface compatibility
/// but does not change the request.
///
/// CONFIRMED LIVE (2026-07-18) against
/// https://router.huggingface.co/hf-inference/models/BAAI/bge-m3/pipeline/feature-extraction:
/// request { "inputs": [...] } → response is a flat JSON array of 1024-dim float arrays, one per
/// input, in order. Requires a fine-grained HF token with "Make calls to Inference Providers" — a
/// plain "Read" token gets a 403 regardless of model.
/// </summary>
internal sealed class HuggingFaceEmbeddingProvider : IEmbeddingProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AiModelOptions _modelOptions;
    private readonly HuggingFaceOptions _options;
    private readonly ILogger<HuggingFaceEmbeddingProvider> _logger;

    public HuggingFaceEmbeddingProvider(
        HttpClient http, IOptions<HuggingFaceOptions> options, IOptions<AiModelOptions> modelOptions, ILogger<HuggingFaceEmbeddingProvider> logger)
    {
        _options = options.Value;
        _modelOptions = modelOptions.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/");
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<Result<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> inputs, EmbeddingPurpose purpose = EmbeddingPurpose.Document, CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
            return Result.Success(new EmbeddingResult([], _modelOptions.EmbeddingModelId, 0));

        var path = $"models/{_modelOptions.EmbeddingModelId}/pipeline/feature-extraction";
        var send = await SendWithRetryAsync(() => BuildJson(path, new EmbedRequest(inputs)), cancellationToken);
        if (send.IsFailure)
            return Result.Failure<EmbeddingResult>(send.Error);

        try
        {
            var vectors = ParseEmbedResponse(send.Value);
            if (vectors.Count != inputs.Count)
                _logger.LogWarning("HF embed returned {Got} vectors for {Expected} inputs", vectors.Count, inputs.Count);
            return Result.Success(new EmbeddingResult(vectors, _modelOptions.EmbeddingModelId, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse HF embed response");
            return Result.Failure<EmbeddingResult>(AppErrors.EmbeddingFailed);
        }
    }

    private static IReadOnlyList<float[]> ParseEmbedResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            throw new JsonException("Unrecognized HF embed response shape (expected top-level array)");

        var list = new List<float[]>(root.GetArrayLength());
        foreach (var row in root.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
                throw new JsonException("Unrecognized HF embed response shape (expected array of arrays)");

            var vector = new float[row.GetArrayLength()];
            var i = 0;
            foreach (var n in row.EnumerateArray())
                vector[i++] = n.GetSingle();
            list.Add(vector);
        }

        return list;
    }

    private HttpRequestMessage BuildJson(string relativePath, object payload) =>
        new(HttpMethod.Post, relativePath) { Content = JsonContent.Create(payload, options: Json) };

    /// <summary>Sends with exponential backoff on 429/5xx — same retry seam as the other AI clients.</summary>
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

                _logger.LogError("HF embed failed ({Status}): {Body}", (int)response.StatusCode, Truncate(body));
                return Result.Failure<string>(AppErrors.AiGatewayError);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt < maxAttempts)
                {
                    await Task.Delay(Backoff(attempt), cancellationToken);
                    continue;
                }
                _logger.LogError(ex, "HF embed threw");
                return Result.Failure<string>(AppErrors.AiGatewayError);
            }
        }
    }

    private static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1) + Random.Shared.Next(0, 100));

    private static string Truncate(string s) => s.Length > 500 ? s[..500] : s;

    private sealed record EmbedRequest(IReadOnlyList<string> Inputs);
}
