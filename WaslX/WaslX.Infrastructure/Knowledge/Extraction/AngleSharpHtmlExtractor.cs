using System.Text.RegularExpressions;
using AngleSharp;
using WaslX.Application.Abstractions.Knowledge;

namespace WaslX.Infrastructure.Knowledge.Extraction;

/// <summary>Parses HTML, strips script/style/noscript, and returns the page title + cleaned body text.</summary>
internal sealed partial class AngleSharpHtmlExtractor : IHtmlContentExtractor
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    public async Task<HtmlContent> ExtractAsync(string html, CancellationToken cancellationToken = default)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken);

        foreach (var el in document.QuerySelectorAll("script, style, noscript"))
            el.Remove();

        var title = document.Title ?? string.Empty;
        var text = WhitespacePattern().Replace(document.Body?.TextContent ?? string.Empty, " ").Trim();
        return new HtmlContent(title, text);
    }
}
