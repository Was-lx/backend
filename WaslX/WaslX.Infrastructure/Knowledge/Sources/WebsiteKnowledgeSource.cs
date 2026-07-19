using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Infrastructure.Knowledge.Sources;

/// <summary>Fetches a website page and extracts its cleaned text. Re-validates the URL against
/// <see cref="SsrfGuard"/> immediately before every fetch (defense-in-depth against DNS rebinding).</summary>
internal sealed class WebsiteKnowledgeSource(HttpClient http, IHtmlContentExtractor htmlExtractor, ILogger<WebsiteKnowledgeSource> logger) : IKnowledgeSource
{
    public KnowledgeSourceType SourceType => KnowledgeSourceType.Website;

    public async IAsyncEnumerable<RawKnowledgeItem> ExtractAsync(
        KnowledgeDocument document, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.SourceUrl))
            yield break;

        if (!SsrfGuard.IsUrlSafe(document.SourceUrl, out var reason))
        {
            logger.LogWarning("Refusing to fetch unsafe URL for document {DocumentId}: {Reason}", document.Id, reason);
            yield break;
        }

        string html;
        try
        {
            html = await http.GetStringAsync(document.SourceUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch website document {DocumentId} from {Url}", document.Id, document.SourceUrl);
            yield break;
        }

        var content = await htmlExtractor.ExtractAsync(html, cancellationToken);
        if (!string.IsNullOrWhiteSpace(content.Text))
            yield return new RawKnowledgeItem(
                content.Text, string.IsNullOrWhiteSpace(content.Title) ? document.Title : content.Title, document.Language.ToString());
    }
}
