using FluentValidation;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.ConnectAccount;

/// <summary>
/// Completes Facebook Login for Business: swaps the authorization code for a long-lived token,
/// resolves the WABA/phone number, and upserts the tenant's WhatsApp account.
/// </summary>
public record ConnectWhatsAppAccountCommand(int? TenantId, string AuthorizationCode, string? WabaId)
    : ICommand<WhatsAppAccountDto>;

public class ConnectWhatsAppAccountCommandHandler(IWhatsAppService whatsAppService)
    : ICommandHandler<ConnectWhatsAppAccountCommand, WhatsAppAccountDto>
{
    public Task<Result<WhatsAppAccountDto>> Handle(ConnectWhatsAppAccountCommand request, CancellationToken cancellationToken) =>
        whatsAppService.ConnectAsync(request.TenantId, request.AuthorizationCode, request.WabaId, cancellationToken);
}

public class ConnectWhatsAppAccountCommandValidator : AbstractValidator<ConnectWhatsAppAccountCommand>
{
    public ConnectWhatsAppAccountCommandValidator()
    {
        RuleFor(x => x.AuthorizationCode)
            .NotEmpty();
    }
}
