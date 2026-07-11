namespace WaslX.Infrastructure.Settings;

/// <summary>
/// Cloudinary account configuration for permanent media storage (WhatsApp's own media URLs
/// are short-lived). Platform-wide secrets — live in configuration, never in the database.
/// </summary>
public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
