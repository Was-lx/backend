namespace WaslX.Infrastructure.Settings;

/// <summary>
/// OpenAI API configuration (platform-wide secret; lives in configuration / user-secrets, never in the
/// database). Used by the summarization pipeline and future AI features.
/// </summary>
public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>OpenAI API key. Supply via user-secrets in dev; empty means the AI features are disabled.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>API base URL including trailing slash.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    /// <summary>Model used for conversation summarization (per PROJECT-CONTEXT: gpt-4.1).</summary>
    public string SummaryModel { get; set; } = "gpt-4.1";

    /// <summary>Optional organization id header.</summary>
    public string? Organization { get; set; }
}
