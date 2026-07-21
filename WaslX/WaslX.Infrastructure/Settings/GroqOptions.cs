namespace WaslX.Infrastructure.Settings;

/// <summary>
/// Groq connection settings for chat/generation. Only used by <c>GroqLlmProvider</c> — the app's
/// embedding provider is unrelated (Hugging Face, see <see cref="HuggingFaceOptions"/>). The single
/// API key is a platform-wide secret — supply via user-secrets/environment variables in production.
/// </summary>
public class GroqOptions
{
    public const string SectionName = "Groq";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatPath { get; set; } = "chat/completions";
    public int TimeoutSeconds { get; set; } = 60;
}
