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
    DateTime? ReviewedAt,
    // HEADER component format: TEXT / IMAGE / VIDEO / DOCUMENT (null when the template has no header).
    // The picker uses this to render a text field vs a media upload for the header parameter.
    string? HeaderFormat = null);

/// <summary>A template button (QUICK_REPLY/URL/PHONE_NUMBER/COPY_CODE/OTP).</summary>
public record TemplateButtonDto(string Type, string? Text, string? Url, string? PhoneNumber);

// ─── Structured send parameters ──────────────────────────────────────────────
// A template send can fill placeholders in three places: the HEADER (text or media),
// the BODY ({{1}}, {{2}}, …), and dynamic BUTTONS (URL suffix or the AUTH OTP code).

/// <summary>Everything needed to fill a template's placeholders on send. All parts optional.</summary>
public record TemplateSendParameters(
    TemplateHeaderParam? Header,
    IReadOnlyList<string>? Body,
    IReadOnlyList<TemplateButtonParam>? Buttons);

/// <summary>
/// Header parameter. <see cref="Kind"/> is "text" (uses <see cref="Text"/>) or a media kind
/// "image"/"video"/"document" (uses <see cref="MediaLink"/>, a public Cloudinary URL).
/// </summary>
public record TemplateHeaderParam(string Kind, string? Text, string? MediaLink);

/// <summary>
/// Dynamic button parameter. <see cref="SubType"/> is "url" (dynamic URL suffix) or "copy_code"
/// (AUTHENTICATION OTP code). <see cref="Index"/> is the zero-based button position.
/// </summary>
public record TemplateButtonParam(int Index, string SubType, string Text);

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
