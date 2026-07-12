using FluentValidation;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.SendTemplateMessage;

/// <summary>Sends a pre-approved WhatsApp template message and persists it as an outbound message.</summary>
public record SendTemplateMessageCommand(int? TenantId, string ToPhone, string TemplateName, string LanguageCode, IReadOnlyList<string>? Variables = null)
    : ICommand<SendMessageResult>;

public class SendTemplateMessageCommandHandler(IWhatsAppService whatsAppService)
    : ICommandHandler<SendTemplateMessageCommand, SendMessageResult>
{
    public Task<Result<SendMessageResult>> Handle(SendTemplateMessageCommand request, CancellationToken cancellationToken) =>
        whatsAppService.SendTemplateAsync(request.TenantId, request.ToPhone, request.TemplateName, request.LanguageCode, request.Variables, cancellationToken: cancellationToken);
}

public class SendTemplateMessageCommandValidator : AbstractValidator<SendTemplateMessageCommand>
{
    public SendTemplateMessageCommandValidator()
    {
        RuleFor(x => x.ToPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
    }
}
