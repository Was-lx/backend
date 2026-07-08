using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.GetAccount;

/// <summary>Returns the calling tenant's WhatsApp connection status (safe fields only).</summary>
public record GetWhatsAppAccountQuery(int? TenantId) : IQuery<WhatsAppAccountDto>;

public class GetWhatsAppAccountQueryHandler(IWhatsAppService whatsAppService)
    : IQueryHandler<GetWhatsAppAccountQuery, WhatsAppAccountDto>
{
    public Task<Result<WhatsAppAccountDto>> Handle(GetWhatsAppAccountQuery request, CancellationToken cancellationToken) =>
        whatsAppService.GetAccountAsync(request.TenantId, cancellationToken);
}
