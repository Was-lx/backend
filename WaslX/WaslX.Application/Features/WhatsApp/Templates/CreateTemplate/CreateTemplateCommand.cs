using FluentValidation;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WhatsApp.Templates.CreateTemplate;

/// <summary>Creates a WhatsApp message template on Meta (Marketing / Utility / Authentication).</summary>
public record CreateTemplateCommand(
    int? TenantId,
    string Name,
    string Category,
    string Language,
    string? HeaderText,
    string? BodyText,
    string? FooterText,
    IReadOnlyList<TemplateButtonInput> Buttons,
    bool AllowCategoryChange = true) : ICommand<TemplateCreateResultDto>;

public class CreateTemplateCommandHandler(IWhatsAppTemplateService templates) : ICommandHandler<CreateTemplateCommand, TemplateCreateResultDto>
{
    public Task<Result<TemplateCreateResultDto>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken) =>
        templates.CreateTemplateAsync(
            request.TenantId,
            new CreateTemplateInput(request.Name, request.Category, request.Language, request.HeaderText, request.BodyText, request.FooterText, request.Buttons ?? [], request.AllowCategoryChange),
            cancellationToken);
}

public class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    private static readonly string[] Categories = ["MARKETING", "UTILITY", "AUTHENTICATION"];

    public CreateTemplateCommandValidator()
    {
        // Meta: lowercase alphanumeric + underscores only, max 512.
        RuleFor(x => x.Name).NotEmpty().MaximumLength(512)
            .Matches("^[a-z0-9_]+$").WithMessage("Name must be lowercase letters, numbers and underscores only.");
        RuleFor(x => x.Category).NotEmpty()
            .Must(c => Categories.Contains(c.ToUpperInvariant())).WithMessage("Category must be MARKETING, UTILITY or AUTHENTICATION.");
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        // Authentication bodies are Meta-generated; every other category needs body text.
        RuleFor(x => x.BodyText).NotEmpty().MaximumLength(1024)
            .When(x => !string.Equals(x.Category, "AUTHENTICATION", StringComparison.OrdinalIgnoreCase));
    }
}
