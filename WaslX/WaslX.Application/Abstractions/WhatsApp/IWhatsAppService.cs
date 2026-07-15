using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Optional step-1 connect-wizard settings captured before the Meta OAuth: a friendly platform
/// name and the number's distribution config. Every field is optional; when absent the connect
/// applies sensible defaults so existing callers keep working.
/// </summary>
public record WhatsAppConnectSettings(
    string? PlatformName = null,
    string? DistributionMode = null,
    bool? DistributeToOffline = null,
    bool? ReassignOnOffline = null,
    int? StartingGroupId = null);

/// <summary>
/// Tenant-facing WhatsApp operations (connect / status / disconnect / send). Owns the
/// persistence of the per-tenant <c>WhatsAppAccount</c> and outbound messages, delegating
/// all Meta HTTP calls to <see cref="IMetaGraphApiService"/>.
/// </summary>
public interface IWhatsAppService
{
    Task<Result<WhatsAppAccountDto>> ConnectAsync(int? tenantId, string authorizationCode, string? wabaId, string? redirectUri = null, WhatsAppConnectSettings? settings = null, CancellationToken cancellationToken = default);
    Task<Result<WhatsAppAccountDto>> GetAccountAsync(int? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lists ALL of the tenant's WhatsApp numbers (light fields only — never the access token).</summary>
    Task<Result<IReadOnlyList<WhatsAppAccountListItemDto>>> GetAccountsAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result> DisconnectAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<SendMessageResult>> SendTextAsync(int? tenantId, string toPhone, string text, int? senderUserId = null, CancellationToken cancellationToken = default);

    /// <param name="parameters">Fill the template HEADER (text/media), BODY ({{1}}, {{2}}, … in order) and dynamic BUTTON placeholders; null for a variable-free template.</param>
    Task<Result<SendMessageResult>> SendTemplateAsync(int? tenantId, string toPhone, string templateName, string languageCode, TemplateSendParameters? parameters = null, int? senderUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Sends an image/video/document already hosted at a public URL (e.g. Cloudinary).</summary>
    Task<Result<SendMessageResult>> SendMediaAsync(
        int? tenantId, string toPhone, string mediaType, string mediaUrl, string? caption, string? fileName,
        string mimeType, int? senderUserId = null, CancellationToken cancellationToken = default);
}
