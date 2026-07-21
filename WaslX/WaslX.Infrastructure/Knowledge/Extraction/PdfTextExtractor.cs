using System.Text;
using UglyToad.PdfPig;
using WaslX.Application.Abstractions.Knowledge;

namespace WaslX.Infrastructure.Knowledge.Extraction;

/// <summary>Extracts text from PDFs page by page. Scanned (image-only) PDFs yield no text — treated
/// upstream as an extraction failure (OCR is out of scope).</summary>
internal sealed class PdfTextExtractor : ITextExtractor
{
    public bool CanHandle(string mimeType) => mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(content);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }
        return Task.FromResult(sb.ToString());
    }
}
