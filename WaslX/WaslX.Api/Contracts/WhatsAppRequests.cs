namespace WaslX.Api.Contracts;

/// <summary>Facebook Login for Business result forwarded from the SPA.</summary>
public record ConnectWhatsAppRequest(string AuthorizationCode, string? WabaId, string? RedirectUri = null);

public record SendWhatsAppTextRequest(string ToPhone, string Text);

public record SendWhatsAppTemplateRequest(string ToPhone, string TemplateName, string LanguageCode);
