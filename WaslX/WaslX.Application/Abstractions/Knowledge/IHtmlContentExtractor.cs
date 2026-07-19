namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>Extracted content from an HTML page: its title plus the cleaned body text.</summary>
public record HtmlContent(string Title, string Text);

public interface IHtmlContentExtractor
{
    Task<HtmlContent> ExtractAsync(string html, CancellationToken cancellationToken = default);
}
