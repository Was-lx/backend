using System.Text;
using WaslX.Application.Abstractions.Knowledge;

namespace WaslX.Infrastructure.Knowledge.Extraction;

/// <summary>Fallback extractor for plain text files (also used when no other extractor matches).</summary>
internal sealed class PlainTextExtractor : ITextExtractor
{
    public bool CanHandle(string mimeType) => mimeType.Equals("text/plain", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(byte[] content, CancellationToken cancellationToken = default) =>
        Task.FromResult(Encoding.UTF8.GetString(content));
}
