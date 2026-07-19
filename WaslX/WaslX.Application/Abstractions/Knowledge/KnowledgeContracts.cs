namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>A raw piece of text extracted from a knowledge source, before normalization/chunking.</summary>
public record RawKnowledgeItem(string Text, string? Title = null, string? Language = null);

/// <summary>Text after Arabic-aware normalization (whitespace, tashkeel, alef/ya/ta-marbuta variants).</summary>
public record NormalizedText(string Text);

/// <summary>One chunk ready to embed — its position in the document and its token count.</summary>
public record TextChunk(int Index, string Text, int TokenCount);
