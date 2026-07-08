using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.Disconnect;

/// <summary>Marks the tenant's WhatsApp account as disconnected.</summary>
public record DisconnectWhatsAppAccountCommand(int? TenantId) : ICommand;

public class DisconnectWhatsAppAccountCommandHandler(IWhatsAppService whatsAppService)
    : ICommandHandler<DisconnectWhatsAppAccountCommand>
{
    public Task<Result> Handle(DisconnectWhatsAppAccountCommand request, CancellationToken cancellationToken) =>
        whatsAppService.DisconnectAsync(request.TenantId, cancellationToken);
}
