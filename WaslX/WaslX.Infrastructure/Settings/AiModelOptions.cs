namespace WaslX.Infrastructure.Settings;

/// <summary>
/// Configurable model ids used by the LLM/embedding providers. **No model name is hardcoded
/// anywhere** — changing the generation or embedding model is a configuration change only.
/// EmbeddingDimensions must match the Qdrant collection vector size (see <see cref="QdrantOptions.VectorSize"/>).
/// </summary>
public class AiModelOptions
{
    public const string SectionName = "AiModels";

    public string GenerationModelId { get; set; } = "llama-3.3-70b-versatile";
    public string EmbeddingModelId { get; set; } = "BAAI/bge-m3";

    /// <summary>Embedding output dimension (BGE-M3 is 1024). Fixed per Qdrant collection.</summary>
    public int EmbeddingDimensions { get; set; } = 1024;
}
