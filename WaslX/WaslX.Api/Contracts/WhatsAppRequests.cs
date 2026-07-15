namespace WaslX.Api.Contracts;

/// <summary>
/// Facebook Login for Business result forwarded from the SPA, plus the optional step-1 wizard
/// fields (friendly platform name + distribution config). All step-1 fields are optional so
/// existing callers that only send the OAuth result keep working.
/// </summary>
public record ConnectWhatsAppRequest(
    string AuthorizationCode,
    string? WabaId,
    string? RedirectUri = null,
    string? PlatformName = null,
    string? DistributionMode = null,
    bool? DistributeToOffline = null,
    bool? ReassignOnOffline = null,
    int? StartingGroupId = null);

public record SendWhatsAppTextRequest(string ToPhone, string Text);

/// <summary>
/// Sends a pre-approved template. <see cref="Header"/> fills a text/media header, <see cref="Body"/>
/// fills the BODY placeholders ({{1}}, {{2}}, … in order), and <see cref="Buttons"/> fills dynamic
/// URL buttons or the AUTHENTICATION OTP code. All parts optional.
/// </summary>
public record SendWhatsAppTemplateRequest(
    string ToPhone,
    string TemplateName,
    string LanguageCode,
    TemplateHeaderParamRequest? Header = null,
    IReadOnlyList<string>? Body = null,
    IReadOnlyList<TemplateButtonParamRequest>? Buttons = null);

/// <summary>Header parameter: Kind is "text" (uses Text) or "image"/"video"/"document" (uses MediaLink).</summary>
public record TemplateHeaderParamRequest(string Kind, string? Text, string? MediaLink);

/// <summary>Dynamic button parameter: SubType "url" (URL suffix) or "copy_code" (AUTH OTP); Index is 0-based.</summary>
public record TemplateButtonParamRequest(int Index, string SubType, string Text);

/// <summary>Body for creating a message template on Meta.</summary>
public record CreateTemplateRequest(
    string Name,
    string Category,
    string Language,
    string? HeaderText,
    string? BodyText,
    string? FooterText,
    IReadOnlyList<CreateTemplateButtonRequest>? Buttons = null,
    bool AllowCategoryChange = true);

public record CreateTemplateButtonRequest(string Type, string Text, string? Url);
