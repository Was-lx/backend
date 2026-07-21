using System.Text;
using System.Text.RegularExpressions;
using WaslX.Application.Abstractions.Knowledge;

namespace WaslX.Infrastructure.Knowledge.Pipeline;

/// <summary>
/// Arabic-aware text normalizer: strips tashkeel (diacritics) and Arabic presentation/RTL control
/// marks, folds common orthography variants (alef forms -> bare alef, ya forms -> bare ya, ta-marbuta
/// -> ha) so retrieval isn't defeated by spelling variance, and collapses whitespace. A no-op for
/// plain Latin text beyond whitespace collapsing.
/// </summary>
internal sealed partial class DefaultTextNormalizer : ITextNormalizer
{
    // Arabic diacritics (tashkeel/harakat) plus tatweel.
    [GeneratedRegex("[\u0640\u064B\u064C\u064D\u064E\u064F\u0650\u0651\u0652]")]
    private static partial Regex TashkeelPattern();

    // Bidi/format control characters that add no semantic value (ZWSP..RLM, LRE..RLO, BOM).
    [GeneratedRegex("[\u200B\u200C\u200D\u200E\u200F\u202A\u202B\u202C\u202D\u202E\uFEFF]")]
    private static partial Regex BidiControlPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    public NormalizedText Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new NormalizedText(string.Empty);

        var s = text.Normalize(NormalizationForm.FormC);
        s = TashkeelPattern().Replace(s, string.Empty);
        s = BidiControlPattern().Replace(s, string.Empty);

        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            sb.Append(ch switch
            {
                'أ' or 'إ' or 'آ' => 'ا', // hamza-alef variants -> bare alef
                'ى' => 'ي',                          // alef maqsura -> ya
                'ة' => 'ه',                          // ta marbuta -> ha
                _ => ch
            });
        }

        var normalized = WhitespacePattern().Replace(sb.ToString(), " ").Trim();
        return new NormalizedText(normalized);
    }
}
