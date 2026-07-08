using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Thin client over the Meta Graph (WhatsApp Cloud) API. Every call returns a
/// <see cref="Result"/> so callers never have to catch transport exceptions.
/// </summary>
public interface IMetaGraphApiService
{
    /// <summary>Exchanges a Facebook Login for Business authorization code for a long-lived access token.</summary>
    Task<Result<MetaTokenResult>> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the WhatsApp Business Account id, phone number id and display phone number
    /// granted to the given access token. Pass <paramref name="wabaId"/> when the frontend
    /// already knows it (Embedded Signup returns it); otherwise it is discovered via debug_token.
    /// </summary>
    Task<Result<MetaBusinessInfo>> GetBusinessInfoAsync(string accessToken, string? wabaId = null, CancellationToken cancellationToken = default);

    /// <summary>Sends a plain text message. Returns the WhatsApp message id (wamid) on success.</summary>
    Task<Result<string>> SendTextMessageAsync(string phoneNumberId, string accessToken, string toPhone, string text, CancellationToken cancellationToken = default);

    /// <summary>Sends a pre-approved template message. Returns the WhatsApp message id (wamid) on success.</summary>
    Task<Result<string>> SendTemplateMessageAsync(string phoneNumberId, string accessToken, string toPhone, string templateName, string languageCode, CancellationToken cancellationToken = default);

    /// <summary>Downloads media bytes for a given media id (two-step: resolve URL, then fetch).</summary>
    Task<Result<MetaMediaResult>> DownloadMediaAsync(string mediaId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Marks an inbound message as read.</summary>
    Task<Result> MarkMessageAsReadAsync(string phoneNumberId, string accessToken, string whatsAppMessageId, CancellationToken cancellationToken = default);
}

/// <summary>Result of a token exchange.</summary>
public record MetaTokenResult(string AccessToken, DateTime? ExpiresAt);

/// <summary>Tenant-scoped business identifiers resolved from Meta.</summary>
public record MetaBusinessInfo(string WhatsAppBusinessAccountId, string PhoneNumberId, string DisplayPhoneNumber);

/// <summary>Downloaded media payload.</summary>
public record MetaMediaResult(byte[] Content, string ContentType);
