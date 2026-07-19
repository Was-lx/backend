namespace WaslX.Infrastructure.Settings;

/// <summary>Tunable knobs for the RAG pipeline (chunking, retrieval, generation, auto-reply caps).</summary>
public class RagOptions
{
    public const string SectionName = "Rag";

    // Chunking
    public int ChunkTokens { get; set; } = 400;
    public int ChunkOverlap { get; set; } = 60;
    public int EmbeddingBatchSize { get; set; } = 96;

    // Retrieval
    public int TopK { get; set; } = 5;
    public double MinScore { get; set; } = 0.35;

    // Generation
    public int MaxContextTokens { get; set; } = 6000;
    public int MaxAnswerTokens { get; set; } = 800;

    // Auto-reply guardrails
    public double AutoReplyMinConfidence { get; set; } = 0.75;
    public int MaxAutoRepliesPerConversation { get; set; } = 3;
}
