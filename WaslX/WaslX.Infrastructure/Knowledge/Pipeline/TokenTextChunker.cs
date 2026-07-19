using Microsoft.Extensions.Options;
using SharpToken;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Knowledge.Pipeline;

/// <summary>
/// Splits text into overlapping token windows sized by <see cref="RagOptions.ChunkTokens"/> /
/// <see cref="RagOptions.ChunkOverlap"/>. Uses a real BPE tokenizer (cl100k) so chunk sizes are
/// predictable regardless of the actual embedding/generation model's own tokenizer.
/// </summary>
internal sealed class TokenTextChunker : ITextChunker
{
    private readonly GptEncoding _encoding = GptEncoding.GetEncoding("cl100k_base");
    private readonly RagOptions _options;

    public TokenTextChunker(IOptions<RagOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var tokens = _encoding.Encode(text);
        if (tokens.Count == 0)
            return [];

        var windowSize = Math.Max(1, _options.ChunkTokens);
        var overlap = Math.Clamp(_options.ChunkOverlap, 0, windowSize - 1);
        var step = windowSize - overlap;

        var chunks = new List<TextChunk>();
        var index = 0;
        for (var start = 0; start < tokens.Count; start += step)
        {
            var length = Math.Min(windowSize, tokens.Count - start);
            var slice = tokens.GetRange(start, length);
            var chunkText = _encoding.Decode(slice).Trim();
            if (chunkText.Length > 0)
                chunks.Add(new TextChunk(index++, chunkText, slice.Count));

            if (start + length >= tokens.Count)
                break;
        }

        return chunks;
    }
}
