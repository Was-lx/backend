using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.WhatsApp;

/// <summary>
/// Meta Graph (WhatsApp Cloud) API client. Uses a typed <see cref="HttpClient"/> and never
/// throws to callers — transport/API failures are logged and surfaced as failed Results.
/// </summary>
internal sealed class MetaGraphApiService : IMetaGraphApiService
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<MetaGraphApiService> _logger;

    public MetaGraphApiService(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<MetaGraphApiService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.ApiBaseUrl.EndsWith('/') ? _options.ApiBaseUrl : _options.ApiBaseUrl + "/");
    }

    public async Task<Result<MetaTokenResult>> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            // FB Login for Business (Embedded Signup) returns a code we swap for a long-lived
            // business-integration system-user token in a single call.
            var url = $"oauth/access_token?client_id={Uri.EscapeDataString(_options.AppId)}" +
                      $"&client_secret={Uri.EscapeDataString(_options.AppSecret)}" +
                      $"&code={Uri.EscapeDataString(code)}";

            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return LogAndFail<MetaTokenResult>(AppErrors.WhatsAppTokenExchangeFailed, "code exchange", body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("access_token", out var tokenEl))
                return LogAndFail<MetaTokenResult>(AppErrors.WhatsAppTokenExchangeFailed, "code exchange (no token)", body);

            DateTime? expiresAt = root.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt64(out var seconds) && seconds > 0
                ? DateTime.UtcNow.AddSeconds(seconds)
                : null;

            return Result.Success(new MetaTokenResult(tokenEl.GetString()!, expiresAt));
        }
        catch (Exception ex)
        {
            return LogAndFail<MetaTokenResult>(AppErrors.WhatsAppGraphApiError, "code exchange", ex);
        }
    }

    public async Task<Result<MetaBusinessInfo>> GetBusinessInfoAsync(string accessToken, string? wabaId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Discover the WABA id from the token's granted scopes when the caller doesn't already have it.
            if (string.IsNullOrWhiteSpace(wabaId))
            {
                var appToken = $"{_options.AppId}|{_options.AppSecret}";
                var debugUrl = $"debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appToken)}";
                using var debugResponse = await _http.GetAsync(debugUrl, cancellationToken);
                var debugBody = await debugResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!debugResponse.IsSuccessStatusCode)
                    return LogAndFail<MetaBusinessInfo>(AppErrors.WhatsAppGraphApiError, "debug_token", debugBody);

                wabaId = ExtractWabaId(debugBody);
                if (string.IsNullOrWhiteSpace(wabaId))
                    return LogAndFail<MetaBusinessInfo>(AppErrors.WhatsAppBusinessInfoFailed, "debug_token (no waba)", debugBody);
            }

            var phoneUrl = $"{wabaId}/phone_numbers?access_token={Uri.EscapeDataString(accessToken)}";
            using var phoneResponse = await _http.GetAsync(phoneUrl, cancellationToken);
            var phoneBody = await phoneResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!phoneResponse.IsSuccessStatusCode)
                return LogAndFail<MetaBusinessInfo>(AppErrors.WhatsAppBusinessInfoFailed, "phone_numbers", phoneBody);

            using var doc = JsonDocument.Parse(phoneBody);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                return LogAndFail<MetaBusinessInfo>(AppErrors.WhatsAppBusinessInfoFailed, "phone_numbers (empty)", phoneBody);

            var first = data[0];
            var phoneNumberId = first.GetProperty("id").GetString()!;
            var displayPhone = first.TryGetProperty("display_phone_number", out var dp) ? dp.GetString() ?? string.Empty : string.Empty;

            return Result.Success(new MetaBusinessInfo(wabaId!, phoneNumberId, displayPhone));
        }
        catch (Exception ex)
        {
            return LogAndFail<MetaBusinessInfo>(AppErrors.WhatsAppGraphApiError, "business info", ex);
        }
    }

    public async Task<Result<string>> SendTextMessageAsync(string phoneNumberId, string accessToken, string toPhone, string text, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toPhone,
            type = "text",
            text = new { preview_url = false, body = text }
        };
        return await PostMessageAsync(phoneNumberId, accessToken, payload, cancellationToken);
    }

    public async Task<Result<string>> SendTemplateMessageAsync(string phoneNumberId, string accessToken, string toPhone, string templateName, string languageCode, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toPhone,
            type = "template",
            template = new { name = templateName, language = new { code = languageCode } }
        };
        return await PostMessageAsync(phoneNumberId, accessToken, payload, cancellationToken);
    }

    public async Task<Result<MetaMediaResult>> DownloadMediaAsync(string mediaId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: resolve the (short-lived, auth-protected) media URL.
            using var metaRequest = new HttpRequestMessage(HttpMethod.Get, mediaId);
            metaRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var metaResponse = await _http.SendAsync(metaRequest, cancellationToken);
            var metaBody = await metaResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!metaResponse.IsSuccessStatusCode)
                return LogAndFail<MetaMediaResult>(AppErrors.WhatsAppGraphApiError, "media lookup", metaBody);

            using var doc = JsonDocument.Parse(metaBody);
            var url = doc.RootElement.GetProperty("url").GetString()!;

            // Step 2: download the bytes (the URL still requires the bearer token).
            using var mediaRequest = new HttpRequestMessage(HttpMethod.Get, url);
            mediaRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var mediaResponse = await _http.SendAsync(mediaRequest, cancellationToken);

            if (!mediaResponse.IsSuccessStatusCode)
                return LogAndFail<MetaMediaResult>(AppErrors.WhatsAppGraphApiError, "media download", mediaResponse.StatusCode.ToString());

            var content = await mediaResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = mediaResponse.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return Result.Success(new MetaMediaResult(content, contentType));
        }
        catch (Exception ex)
        {
            return LogAndFail<MetaMediaResult>(AppErrors.WhatsAppGraphApiError, "media download", ex);
        }
    }

    public async Task<Result> MarkMessageAsReadAsync(string phoneNumberId, string accessToken, string whatsAppMessageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { messaging_product = "whatsapp", status = "read", message_id = whatsAppMessageId };
            using var request = BuildJsonRequest($"{phoneNumberId}/messages", accessToken, payload);
            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return LogAndFail(AppErrors.WhatsAppGraphApiError, "mark read", body);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return LogAndFail(AppErrors.WhatsAppGraphApiError, "mark read", ex);
        }
    }

    private async Task<Result<string>> PostMessageAsync(string phoneNumberId, string accessToken, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildJsonRequest($"{phoneNumberId}/messages", accessToken, payload);
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return LogAndFail<string>(AppErrors.WhatsAppSendFailed, "send message", body);

            using var doc = JsonDocument.Parse(body);
            var wamid = doc.RootElement.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0
                ? messages[0].GetProperty("id").GetString() ?? string.Empty
                : string.Empty;

            return Result.Success(wamid);
        }
        catch (Exception ex)
        {
            return LogAndFail<string>(AppErrors.WhatsAppGraphApiError, "send message", ex);
        }
    }

    private static HttpRequestMessage BuildJsonRequest(string relativeUrl, string accessToken, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    /// <summary>Reads debug_token output and returns the WABA id from the whatsapp_business_management scope.</summary>
    private static string? ExtractWabaId(string debugTokenBody)
    {
        using var doc = JsonDocument.Parse(debugTokenBody);
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("granular_scopes", out var scopes) ||
            scopes.ValueKind != JsonValueKind.Array)
            return null;

        // Prefer the management scope; fall back to any scope that carries target ids.
        string? fallback = null;
        foreach (var scope in scopes.EnumerateArray())
        {
            if (!scope.TryGetProperty("target_ids", out var targets) || targets.GetArrayLength() == 0)
                continue;

            var id = targets[0].GetString();
            var name = scope.TryGetProperty("scope", out var s) ? s.GetString() : null;
            if (name == "whatsapp_business_management")
                return id;
            fallback ??= id;
        }

        return fallback;
    }

    private Result LogAndFail(Error error, string operation, string detail)
    {
        _logger.LogError("Meta Graph API {Operation} failed: {Detail}", operation, detail);
        return Result.Failure(error);
    }

    private Result LogAndFail(Error error, string operation, Exception ex)
    {
        _logger.LogError(ex, "Meta Graph API {Operation} threw", operation);
        return Result.Failure(error);
    }

    private Result<T> LogAndFail<T>(Error error, string operation, string detail)
    {
        _logger.LogError("Meta Graph API {Operation} failed: {Detail}", operation, detail);
        return Result.Failure<T>(error);
    }

    private Result<T> LogAndFail<T>(Error error, string operation, Exception ex)
    {
        _logger.LogError(ex, "Meta Graph API {Operation} threw", operation);
        return Result.Failure<T>(error);
    }
}
