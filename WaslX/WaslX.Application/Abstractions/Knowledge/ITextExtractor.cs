namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>Extracts plain text from a file's bytes. Resolved by MIME type (first match wins).</summary>
public interface ITextExtractor
{
    bool CanHandle(string mimeType);
    Task<string> ExtractAsync(byte[] content, CancellationToken cancellationToken = default);
}
