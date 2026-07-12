namespace WaslX.Api.Contracts;

/// <summary>Facebook Login for Business result forwarded from the SPA.</summary>
public record ConnectWhatsAppRequest(string AuthorizationCode, string? WabaId, string? RedirectUri = null);

public record SendWhatsAppTextRequest(string ToPhone, string Text);

/// <summary><see cref="Variables"/> fill the template BODY placeholders ({{1}}, {{2}}, … in order).</summary>
public record SendWhatsAppTemplateRequest(string ToPhone, string TemplateName, string LanguageCode, IReadOnlyList<string>? Variables = null);

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
