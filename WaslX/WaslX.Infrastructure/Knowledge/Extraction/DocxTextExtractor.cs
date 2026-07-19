using DocumentFormat.OpenXml.Packaging;
using WaslX.Application.Abstractions.Knowledge;

namespace WaslX.Infrastructure.Knowledge.Extraction;

/// <summary>Extracts the flattened body text of a .docx file.</summary>
internal sealed class DocxTextExtractor : ITextExtractor
{
    private const string DocxMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool CanHandle(string mimeType) => mimeType.Equals(DocxMimeType, StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        return Task.FromResult(body?.InnerText ?? string.Empty);
    }
}
