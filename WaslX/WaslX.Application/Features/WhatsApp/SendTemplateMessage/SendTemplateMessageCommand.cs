using FluentValidation;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.SendTemplateMessage;

/// <summary>Sends a pre-approved WhatsApp template message and persists it as an outbound message.</summary>
public record SendTemplateMessageCommand(int? TenantId, string ToPhone, string TemplateName, string LanguageCode, TemplateSendParameters? Parameters = null)
    : ICommand<SendMessageResult>;

public class SendTemplateMessageCommandHandler(IWhatsAppService whatsAppService)
    : ICommandHandler<SendTemplateMessageCommand, SendMessageResult>
{
    public Task<Result<SendMessageResult>> Handle(SendTemplateMessageCommand request, CancellationToken cancellationToken) =>
        whatsAppService.SendTemplateAsync(request.TenantId, request.ToPhone, request.TemplateName, request.LanguageCode, request.Parameters, cancellationToken: cancellationToken);
}

public class SendTemplateMessageCommandValidator : AbstractValidator<SendTemplateMessageCommand>
{
    private static readonly string[] HeaderKinds = ["text", "image", "video", "document"];
    private static readonly string[] ButtonSubTypes = ["url", "copy_code"];

    public SendTemplateMessageCommandValidator()
    {
        RuleFor(x => x.ToPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);

        When(x => x.Parameters?.Header is not null, () =>
        {
            RuleFor(x => x.Parameters!.Header!.Kind)
                .Must(k => HeaderKinds.Contains(k, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Header kind must be text, image, video or document.");
            // Text headers need Text; media headers need a link.
            RuleFor(x => x.Parameters!.Header!.Text)
                .NotEmpty().When(x => string.Equals(x.Parameters!.Header!.Kind, "text", StringComparison.OrdinalIgnoreCase));
            RuleFor(x => x.Parameters!.Header!.MediaLink)
                .NotEmpty().When(x => !string.Equals(x.Parameters!.Header!.Kind, "text", StringComparison.OrdinalIgnoreCase));
        });

        RuleForEach(x => x.Parameters!.Body)
            .NotEmpty().When(x => x.Parameters?.Body is not null)
            .WithMessage("Body parameters cannot be empty.");

        When(x => x.Parameters?.Buttons is not null, () =>
        {
            RuleForEach(x => x.Parameters!.Buttons).ChildRules(b =>
            {
                b.RuleFor(p => p.SubType).Must(s => ButtonSubTypes.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .WithMessage("Button sub-type must be url or copy_code.");
                b.RuleFor(p => p.Text).NotEmpty();
                b.RuleFor(p => p.Index).GreaterThanOrEqualTo(0);
            });
        });
    }
}
