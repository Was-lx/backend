using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.GetAccounts;

/// <summary>Returns ALL of the calling tenant's WhatsApp numbers as a light list (safe fields only).</summary>
public record GetWhatsAppAccountsQuery(int? TenantId) : IQuery<IReadOnlyList<WhatsAppAccountListItemDto>>;

public class GetWhatsAppAccountsQueryHandler(IWhatsAppService whatsAppService)
    : IQueryHandler<GetWhatsAppAccountsQuery, IReadOnlyList<WhatsAppAccountListItemDto>>
{
    public Task<Result<IReadOnlyList<WhatsAppAccountListItemDto>>> Handle(GetWhatsAppAccountsQuery request, CancellationToken cancellationToken) =>
        whatsAppService.GetAccountsAsync(request.TenantId, cancellationToken);
}
