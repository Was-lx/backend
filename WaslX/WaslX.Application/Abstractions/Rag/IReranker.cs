namespace WaslX.Application.Abstractions.Rag;

/// <summary>
/// Reorders/trims retrieved chunks after the initial ANN search. Starts as score-threshold +
/// content-diversity (MMR-style, text-based); a cross-encoder/LLM reranker can implement this same
/// seam later without touching the retriever.
/// </summary>
public interface IReranker
{
    IReadOnlyList<RetrievedChunk> Rerank(IReadOnlyList<RetrievedChunk> chunks, int topK);
}
