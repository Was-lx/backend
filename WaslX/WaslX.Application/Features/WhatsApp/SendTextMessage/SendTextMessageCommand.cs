using FluentValidation;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.SendTextMessage;

/// <summary>Sends a plain text WhatsApp message to a customer and persists it as an outbound message.</summary>
public record SendTextMessageCommand(int? TenantId, string ToPhone, string Text) : ICommand<SendMessageResult>;

public class SendTextMessageCommandHandler(IWhatsAppService whatsAppService)
    : ICommandHandler<SendTextMessageCommand, SendMessageResult>
{
    public Task<Result<SendMessageResult>> Handle(SendTextMessageCommand request, CancellationToken cancellationToken) =>
        whatsAppService.SendTextAsync(request.TenantId, request.ToPhone, request.Text, cancellationToken);
}

public class SendTextMessageCommandValidator : AbstractValidator<SendTextMessageCommand>
{
    public SendTextMessageCommandValidator()
    {
        RuleFor(x => x.ToPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4096);
    }
}
