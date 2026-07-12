using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.WhatsApp;

/// <summary>
/// Tenant-facing message-template operations. Resolves the tenant's connected WhatsApp account
/// (WABA id + access token) and proxies Meta's <c>/message_templates</c> API — Meta is the source
/// of truth for a template's approval status, so nothing is mirrored locally.
/// </summary>
public interface IWhatsAppTemplateService
{
    Task<Result<IReadOnlyList<TemplateDto>>> GetTemplatesAsync(int? tenantId, string? status, CancellationToken cancellationToken = default);

    Task<Result<TemplateCreateResultDto>> CreateTemplateAsync(int? tenantId, CreateTemplateInput input, CancellationToken cancellationToken = default);
}
