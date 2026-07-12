using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.Templates.GetTemplates;

/// <summary>Lists the tenant's message templates from Meta, optionally filtered by status (e.g. "APPROVED").</summary>
public record GetTemplatesQuery(int? TenantId, string? Status) : IQuery<IReadOnlyList<TemplateDto>>;

public class GetTemplatesQueryHandler(IWhatsAppTemplateService templates) : IQueryHandler<GetTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    public Task<Result<IReadOnlyList<TemplateDto>>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken) =>
        templates.GetTemplatesAsync(request.TenantId, request.Status, cancellationToken);
}
