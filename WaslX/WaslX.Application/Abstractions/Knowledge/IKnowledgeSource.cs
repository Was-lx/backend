using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>
/// A source of ingestable knowledge (FAQ, uploaded document, website page, ...). Each concrete
/// source is a thin adapter — all normalization/chunking/embedding/indexing is shared, driven by
/// <see cref="IKnowledgeIngestionPipeline"/>. Resolved by <see cref="KnowledgeDocument.SourceType"/>.
/// </summary>
public interface IKnowledgeSource
{
    KnowledgeSourceType SourceType { get; }

    /// <summary>Pulls the raw text for this document from its origin (DB row, file, URL, ...).</summary>
    IAsyncEnumerable<RawKnowledgeItem> ExtractAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
}
