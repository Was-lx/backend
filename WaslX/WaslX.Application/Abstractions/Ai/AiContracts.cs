namespace WaslX.Application.Abstractions.Ai;

/// <summary>A single turn in an LLM conversation. Role is provider-agnostic ("user"/"assistant").</summary>
public record LlmMessage(string Role, string Content);

/// <summary>
/// A model-agnostic generation request. Carries no vendor- or model-specific fields — the concrete
/// provider maps this onto whatever the gateway expects and injects the configured model id.
/// </summary>
public record LlmRequest(
    string System,
    IReadOnlyList<LlmMessage> Messages,
    double? Temperature = null,
    int? MaxTokens = null);

/// <summary>Result of a non-streaming generation.</summary>
public record LlmResult(string Text, string ModelId, int InputTokens, int OutputTokens);

/// <summary>One incremental piece of a streamed generation.</summary>
public record LlmStreamChunk(string DeltaText, bool IsFinal);

/// <summary>Result of an embedding batch: one vector per input, in order.</summary>
public record EmbeddingResult(IReadOnlyList<float[]> Vectors, string ModelId, int TotalTokens);

/// <summary>
/// Why we are embedding — asymmetric models (e.g. Cohere) encode documents and queries differently
/// (input_type "search_document" vs "search_query"). Ingestion uses Document; retrieval uses Query.
/// </summary>
public enum EmbeddingPurpose
{
    Document,
    Query
}
