using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services.Knowledge.Sources;

/// <summary>Extracts a FAQ's question+answer as one embeddable item, via its SourceRefId.</summary>
internal sealed class FaqKnowledgeSource(ApplicationDbContext db) : IKnowledgeSource
{
    public KnowledgeSourceType SourceType => KnowledgeSourceType.FAQ;

    public async IAsyncEnumerable<RawKnowledgeItem> ExtractAsync(
        KnowledgeDocument document, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (document.SourceRefId is not { } faqId)
            yield break;

        var faq = await db.FAQs.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == faqId && f.TenantId == document.TenantId, cancellationToken);
        if (faq is null || !faq.IsActive)
            yield break;

        yield return new RawKnowledgeItem($"{faq.Question}\n{faq.Answer}", faq.Question, faq.Language.ToString());
    }
}
