namespace WaslX.Infrastructure.Settings;

/// <summary>Qdrant vector-store connection + collection settings.</summary>
public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    /// <summary>Qdrant host, e.g. "localhost". Used with the gRPC client.</summary>
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 6334;

    public bool UseTls { get; set; } = false;

    /// <summary>API key (optional for local dev; required for Qdrant Cloud).</summary>
    public string? ApiKey { get; set; }

    public string Collection { get; set; } = "waslx_knowledge";

    /// <summary>Must equal <see cref="AiModelOptions.EmbeddingDimensions"/>.</summary>
    public int VectorSize { get; set; } = 1536;

    /// <summary>Distance metric — Cosine for normalized embeddings.</summary>
    public string Distance { get; set; } = "Cosine";
}
