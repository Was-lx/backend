using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Thin client over the Meta Graph (WhatsApp Cloud) API. Every call returns a
/// <see cref="Result"/> so callers never have to catch transport exceptions.
/// </summary>
public interface IMetaGraphApiService
{
    /// <summary>
    /// Exchanges a Facebook Login for Business authorization code for a long-lived access token.
    /// Pass <paramref name="redirectUri"/> when the code was obtained via a real browser redirect
    /// (manual OAuth flow) — it must match the redirect_uri used on the authorization request.
    /// </summary>
    Task<Result<MetaTokenResult>> ExchangeCodeForTokenAsync(string code, string? redirectUri = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the WhatsApp Business Account id, phone number id and display phone number
    /// granted to the given access token. Pass <paramref name="wabaId"/> when the frontend
    /// already knows it (Embedded Signup returns it); otherwise it is discovered via debug_token.
    /// </summary>
    Task<Result<MetaBusinessInfo>> GetBusinessInfoAsync(string accessToken, string? wabaId = null, CancellationToken cancellationToken = default);

    /// <summary>Sends a plain text message. Returns the WhatsApp message id (wamid) on success.</summary>
    Task<Result<string>> SendTextMessageAsync(string phoneNumberId, string accessToken, string toPhone, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an image/video/document by public link (Cloudinary URL) — WhatsApp fetches it itself,
    /// no separate media-upload step against the Graph API is needed. Returns the WhatsApp message id.
    /// </summary>
    Task<Result<string>> SendMediaMessageAsync(
        string phoneNumberId, string accessToken, string toPhone, string mediaType, string mediaUrl,
        string? caption, string? fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a pre-approved template message. <paramref name="parameters"/> fill the template's
    /// HEADER (text or media), BODY ({{1}}, {{2}}, … in order) and dynamic BUTTON placeholders;
    /// pass null for a template with no variables. Returns the WhatsApp message id (wamid) on success.
    /// </summary>
    Task<Result<string>> SendTemplateMessageAsync(
        string phoneNumberId, string accessToken, string toPhone, string templateName, string languageCode,
        TemplateSendParameters? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Lists the WABA's message templates, optionally filtered by status (e.g. "APPROVED").</summary>
    Task<Result<IReadOnlyList<MetaTemplate>>> ListTemplatesAsync(
        string wabaId, string accessToken, string? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a message template (POST {waba-id}/message_templates). <paramref name="payload"/> is the
    /// fully-shaped request body ({ name, language, category, components }). Returns the new id + status.
    /// </summary>
    Task<Result<MetaTemplateCreateResult>> CreateTemplateAsync(
        string wabaId, string accessToken, object payload, CancellationToken cancellationToken = default);

    /// <summary>Downloads media bytes for a given media id (two-step: resolve URL, then fetch).</summary>
    Task<Result<MetaMediaResult>> DownloadMediaAsync(string mediaId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Marks an inbound message as read.</summary>
    Task<Result> MarkMessageAsReadAsync(string phoneNumberId, string accessToken, string whatsAppMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes this WABA to the App's webhook (POST {waba-id}/subscribed_apps). Without this,
    /// Meta never delivers inbound message/status events for the account to our callback URL,
    /// even when the App-level webhook Callback URL/Verify Token are correctly configured.
    /// </summary>
    Task<Result> SubscribeToWebhooksAsync(string wabaId, string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>Result of a token exchange.</summary>
public record MetaTokenResult(string AccessToken, DateTime? ExpiresAt);

/// <summary>Tenant-scoped business identifiers resolved from Meta.</summary>
public record MetaBusinessInfo(string WhatsAppBusinessAccountId, string PhoneNumberId, string DisplayPhoneNumber);

/// <summary>Downloaded media payload.</summary>
public record MetaMediaResult(byte[] Content, string ContentType);

/// <summary>A message template as returned by Meta (id, name, language, category, approval status, components).</summary>
public record MetaTemplate(
    string Id,
    string Name,
    string Language,
    string Category,
    string Status,
    IReadOnlyList<MetaTemplateComponent> Components);

/// <summary>One template component (HEADER/BODY/FOOTER/BUTTONS).</summary>
public record MetaTemplateComponent(string Type, string? Format, string? Text, IReadOnlyList<MetaTemplateButton> Buttons);

/// <summary>One template button (QUICK_REPLY/URL/PHONE_NUMBER/COPY_CODE/OTP).</summary>
public record MetaTemplateButton(string Type, string? Text, string? Url, string? PhoneNumber);

/// <summary>Result of creating a template — Meta assigns an id and an initial (usually PENDING) status.</summary>
public record MetaTemplateCreateResult(string Id, string Status, string Category);
