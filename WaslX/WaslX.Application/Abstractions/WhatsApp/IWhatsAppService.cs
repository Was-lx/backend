using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Tenant-facing WhatsApp operations (connect / status / disconnect / send). Owns the
/// persistence of the per-tenant <c>WhatsAppAccount</c> and outbound messages, delegating
/// all Meta HTTP calls to <see cref="IMetaGraphApiService"/>.
/// </summary>
public interface IWhatsAppService
{
    Task<Result<WhatsAppAccountDto>> ConnectAsync(int? tenantId, string authorizationCode, string? wabaId, string? redirectUri = null, CancellationToken cancellationToken = default);
    Task<Result<WhatsAppAccountDto>> GetAccountAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result> DisconnectAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<SendMessageResult>> SendTextAsync(int? tenantId, string toPhone, string text, int? senderUserId = null, CancellationToken cancellationToken = default);
    Task<Result<SendMessageResult>> SendTemplateAsync(int? tenantId, string toPhone, string templateName, string languageCode, CancellationToken cancellationToken = default);

    /// <summary>Sends an image/video/document already hosted at a public URL (e.g. Cloudinary).</summary>
    Task<Result<SendMessageResult>> SendMediaAsync(
        int? tenantId, string toPhone, string mediaType, string mediaUrl, string? caption, string? fileName,
        string mimeType, int? senderUserId = null, CancellationToken cancellationToken = default);
}
