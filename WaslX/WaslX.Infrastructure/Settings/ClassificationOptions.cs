namespace WaslX.Infrastructure.Settings;

/// <summary>
/// Top-level classification configuration section.
/// The concrete AI provider implementation is registered separately in DependencyInjection.
/// </summary>
public sealed class ClassificationOptions
{
    public const string SectionName = "Classification";

    /// <summary>
    /// Logical name of the active AI provider (informational only — used for logging / metrics).
    /// Example values: "OpenAI", "Groq", "Gemini".
    /// </summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// Fall back to the rule-based classifier when the primary AI provider fails.
    /// </summary>
    public bool UseFallbackOnError { get; set; } = true;
}
