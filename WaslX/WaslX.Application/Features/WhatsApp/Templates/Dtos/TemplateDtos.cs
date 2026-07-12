namespace WaslX.Application.Features.WhatsApp.Templates.Dtos;

/// <summary>A WhatsApp message template (projected from Meta) for listing + the composer picker.</summary>
public record TemplateDto(
    string Id,
    string Name,
    string Language,
    string Category,
    string Status,
    string? HeaderText,
    string? BodyText,
    string? FooterText,
    IReadOnlyList<TemplateButtonDto> Buttons,
    string? ReasonCode,
    string? ReasonText,
    string? MetaNotes,
    string? SubmittedCategory,
    string? FinalCategory,
    bool AllowCategoryChange,
    bool ChangedByMeta,
    DateTime? ReviewedAt);

/// <summary>A template button (QUICK_REPLY/URL/PHONE_NUMBER/COPY_CODE/OTP).</summary>
public record TemplateButtonDto(string Type, string? Text, string? Url, string? PhoneNumber);

/// <summary>Result of creating a template — Meta assigns an id and an initial (usually PENDING) status.</summary>
public record TemplateCreateResultDto(string Id, string Status, string Category);

/// <summary>Input for creating a template (Functional depth: header text, body+variables, footer, quick-reply/URL buttons).</summary>
public record CreateTemplateInput(
    string Name,
    string Category,
    string Language,
    string? HeaderText,
    string? BodyText,
    string? FooterText,
    IReadOnlyList<TemplateButtonInput> Buttons,
    bool AllowCategoryChange = true);

/// <summary>A button to add when creating a template.</summary>
public record TemplateButtonInput(string Type, string Text, string? Url);
