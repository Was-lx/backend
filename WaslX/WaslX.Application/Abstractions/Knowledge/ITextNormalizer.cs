namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>Normalizes raw extracted text before chunking (whitespace, Arabic orthography variants).</summary>
public interface ITextNormalizer
{
    NormalizedText Normalize(string text);
}
