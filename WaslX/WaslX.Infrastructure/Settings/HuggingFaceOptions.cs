namespace WaslX.Infrastructure.Settings;

/// <summary>
/// Hugging Face Inference Providers settings, used only by <c>HuggingFaceEmbeddingProvider</c> for
/// embeddings. Requires a fine-grained token with "Make calls to Inference Providers" enabled — a
/// plain "Read" token gets a 403 regardless of model.
/// </summary>
public class HuggingFaceOptions
{
    public const string SectionName = "HuggingFace";

    public string BaseUrl { get; set; } = "https://router.huggingface.co/hf-inference/";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}
