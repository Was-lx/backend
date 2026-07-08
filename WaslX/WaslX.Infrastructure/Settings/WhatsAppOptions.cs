namespace WaslX.Infrastructure.Settings;

/// <summary>
/// App-level Meta / WhatsApp Cloud API configuration. These are platform-wide secrets
/// (not tenant-specific) and live in configuration, never in the database.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsAppOptions";

    /// <summary>Meta App ID (client_id) used for the Facebook Login for Business flow.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Meta App Secret (client_secret). Used for token exchange and webhook signature validation.</summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>Graph API version, e.g. "v25.0".</summary>
    public string GraphApiVersion { get; set; } = "v25.0";

    /// <summary>Token echoed back to Meta during webhook (GET) verification.</summary>
    public string WebhookVerifyToken { get; set; } = string.Empty;

    /// <summary>Graph API base URL including version, e.g. "https://graph.facebook.com/v25.0/".</summary>
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com/v25.0/";
}
