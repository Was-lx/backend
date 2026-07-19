using WaslX.Application.Abstractions.Rag;

namespace WaslX.Infrastructure.Rag;

/// <summary>
/// Score-ordered selection with a diversity guard: skips a candidate whose content is a near-duplicate
/// (high word-overlap) of an already-selected chunk, so the top-K isn't dominated by repeats of the
/// same FAQ/paragraph. Operates on chunk text (no raw vectors are returned from Qdrant on the hot
/// path) — a true MMR-over-embeddings or a cross-encoder can replace this later behind the same seam.
/// </summary>
internal sealed class MmrReranker : IReranker
{
    private const double DuplicateOverlapThreshold = 0.75;

    public IReadOnlyList<RetrievedChunk> Rerank(IReadOnlyList<RetrievedChunk> chunks, int topK)
    {
        var ordered = chunks.OrderByDescending(c => c.Score).ToList();
        var selected = new List<RetrievedChunk>(Math.Min(topK, ordered.Count));
        var selectedWordSets = new List<HashSet<string>>();

        foreach (var candidate in ordered)
        {
            if (selected.Count >= topK)
                break;

            var words = Tokenize(candidate.Content);
            var isDuplicate = selectedWordSets.Any(existing => JaccardOverlap(existing, words) >= DuplicateOverlapThreshold);
            if (isDuplicate)
                continue;

            selected.Add(candidate);
            selectedWordSets.Add(words);
        }

        return selected;
    }

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

    private static double JaccardOverlap(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
            return 0;
        var intersection = a.Count(b.Contains);
        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }
}
