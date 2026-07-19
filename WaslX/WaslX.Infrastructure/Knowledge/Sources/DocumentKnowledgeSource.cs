using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Infrastructure.Knowledge.Sources;

/// <summary>
/// Extracts text from an uploaded document (PDF/DOCX/TXT hosted on Cloudinary). Downloads the file
/// by its stored URL and dispatches to the extractor matching its MIME type.
/// </summary>
internal sealed class DocumentKnowledgeSource(HttpClient http, IEnumerable<ITextExtractor> extractors, ILogger<DocumentKnowledgeSource> logger) : IKnowledgeSource
{
    public KnowledgeSourceType SourceType => KnowledgeSourceType.Document;

    public async IAsyncEnumerable<RawKnowledgeItem> ExtractAsync(
        KnowledgeDocument document, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.FileUrl) || string.IsNullOrWhiteSpace(document.MimeType))
            yield break;

        byte[] bytes;
        try
        {
            bytes = await http.GetByteArrayAsync(document.FileUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to download knowledge document {DocumentId} from {Url}", document.Id, document.FileUrl);
            throw new InvalidOperationException($"Failed to download the file: {ex.Message}", ex);
        }

        var extractor = extractors.FirstOrDefault(e => e.CanHandle(document.MimeType!))
            ?? extractors.FirstOrDefault(e => e.CanHandle("text/plain"));
        if (extractor is null)
            throw new InvalidOperationException($"No text extractor registered for MIME type {document.MimeType}");

        string text;
        try
        {
            text = await extractor.ExtractAsync(bytes, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract text from knowledge document {DocumentId}", document.Id);
            throw new InvalidOperationException($"Failed to extract text from the file: {ex.Message}", ex);
        }

        if (!string.IsNullOrWhiteSpace(text))
            yield return new RawKnowledgeItem(text, document.Title, document.Language.ToString());
    }
}
