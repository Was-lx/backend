namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>Splits normalized text into token-windowed chunks (size/overlap from RagOptions).</summary>
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}
