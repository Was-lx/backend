using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Application.Abstractions.Rag;
using WaslX.Application.Features.Knowledge.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services.Knowledge;

internal sealed class KnowledgeService(ApplicationDbContext db, IVectorStore vectorStore) : IKnowledgeService
{
    // ────────────────────────────────────────────────── FAQs ──

    public async Task<Result<KnowledgePagedResult<FaqResponse>>> GetFaqsAsync(
        int? tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<KnowledgePagedResult<FaqResponse>>(AppErrors.NoTenantContext);

        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await db.FAQs.AsNoTracking()
            .Where(f => f.TenantId == tid)
            .OrderByDescending(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(f => new
            {
                Faq = f,
                Document = db.KnowledgeDocuments
                    .Where(d => d.TenantId == tid && d.SourceType == KnowledgeSourceType.FAQ && d.SourceRefId == f.Id)
                    .Select(d => new { d.Id, d.Status })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var items = rows.Select(r => new FaqResponse(
            r.Faq.Id, r.Faq.Question, r.Faq.Answer, r.Faq.Language.ToString(), r.Faq.IsActive,
            r.Document?.Id, r.Document?.Status.ToString())).ToList();

        return Result.Success(new KnowledgePagedResult<FaqResponse>(items, hasMore));
    }

    public async Task<Result<KnowledgeMutationResult>> CreateFaqAsync(
        int? tenantId, UpsertFaqRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<KnowledgeMutationResult>(AppErrors.NoTenantContext);

        var language = ParseLanguage(request.Language);
        var faq = new FAQ { TenantId = tid, Question = request.Question, Answer = request.Answer, Language = language, IsActive = request.IsActive };
        db.FAQs.Add(faq);
        await db.SaveChangesAsync(cancellationToken);

        var document = new KnowledgeDocument
        {
            TenantId = tid,
            SourceType = KnowledgeSourceType.FAQ,
            SourceRefId = faq.Id,
            Title = request.Question,
            Language = language,
            Status = KnowledgeDocumentStatus.Pending
        };
        db.KnowledgeDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new KnowledgeMutationResult(faq.Id, document.Id));
    }

    public async Task<Result<KnowledgeMutationResult>> UpdateFaqAsync(
        int? tenantId, int faqId, UpsertFaqRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<KnowledgeMutationResult>(AppErrors.NoTenantContext);

        var faq = await db.FAQs.FirstOrDefaultAsync(f => f.Id == faqId && f.TenantId == tid, cancellationToken);
        if (faq is null)
            return Result.Failure<KnowledgeMutationResult>(AppErrors.FaqNotFound);

        var language = ParseLanguage(request.Language);
        faq.Question = request.Question;
        faq.Answer = request.Answer;
        faq.Language = language;
        faq.IsActive = request.IsActive;
        faq.UpdatedAt = DateTime.UtcNow;

        var document = await db.KnowledgeDocuments.FirstOrDefaultAsync(
            d => d.TenantId == tid && d.SourceType == KnowledgeSourceType.FAQ && d.SourceRefId == faqId, cancellationToken);
        if (document is null)
        {
            document = new KnowledgeDocument { TenantId = tid, SourceType = KnowledgeSourceType.FAQ, SourceRefId = faqId };
            db.KnowledgeDocuments.Add(document);
        }
        document.Title = request.Question;
        document.Language = language;
        document.Status = KnowledgeDocumentStatus.Pending; // content changed — re-index on next job run

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(new KnowledgeMutationResult(faq.Id, document.Id));
    }

    public async Task<Result> DeleteFaqAsync(int? tenantId, int faqId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var faq = await db.FAQs.FirstOrDefaultAsync(f => f.Id == faqId && f.TenantId == tid, cancellationToken);
        if (faq is null)
            return Result.Failure(AppErrors.FaqNotFound);

        var document = await db.KnowledgeDocuments.Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.TenantId == tid && d.SourceType == KnowledgeSourceType.FAQ && d.SourceRefId == faqId, cancellationToken);
        if (document is not null)
            await RemoveDocumentAsync(document, cancellationToken);

        db.FAQs.Remove(faq);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ────────────────────────────────────────────────── Documents (generic) ──

    public async Task<Result<KnowledgePagedResult<KnowledgeDocumentResponse>>> GetDocumentsAsync(
        int? tenantId, KnowledgeSourceType? sourceType, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<KnowledgePagedResult<KnowledgeDocumentResponse>>(AppErrors.NoTenantContext);

        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.KnowledgeDocuments.AsNoTracking().Where(d => d.TenantId == tid);
        if (sourceType is { } st)
            query = query.Where(d => d.SourceType == st);

        var rows = await query
            .OrderByDescending(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(d => new KnowledgeDocumentResponse(
                d.Id, d.SourceType.ToString(), d.Title, d.Language.ToString(), d.Status.ToString(), d.ErrorMessage,
                d.ChunkCount, d.Version, d.UpdatedAt, d.FileUrl, d.FileName, d.SourceUrl))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        return Result.Success(new KnowledgePagedResult<KnowledgeDocumentResponse>(rows, hasMore));
    }

    public async Task<Result> DeleteDocumentAsync(int? tenantId, int documentId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var document = await db.KnowledgeDocuments.Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tid, cancellationToken);
        if (document is null)
            return Result.Failure(AppErrors.KnowledgeDocumentNotFound);

        // A FAQ-sourced document is deleted together with its FAQ row, not standalone.
        if (document.SourceType == KnowledgeSourceType.FAQ && document.SourceRefId is { } faqId)
        {
            var faq = await db.FAQs.FirstOrDefaultAsync(f => f.Id == faqId && f.TenantId == tid, cancellationToken);
            if (faq is not null)
                db.FAQs.Remove(faq);
        }

        await RemoveDocumentAsync(document, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<int>> PrepareReindexAsync(int? tenantId, int documentId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<int>(AppErrors.NoTenantContext);

        var document = await db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tid, cancellationToken);
        if (document is null)
            return Result.Failure<int>(AppErrors.KnowledgeDocumentNotFound);

        document.Status = KnowledgeDocumentStatus.Pending;
        document.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(document.Id);
    }

    public async Task<Result<int>> CreateDocumentAsync(
        int? tenantId, KnowledgeSourceType sourceType, string title, Language language,
        string? fileUrl = null, string? fileName = null, string? mimeType = null, string? sourceUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<int>(AppErrors.NoTenantContext);

        var document = new KnowledgeDocument
        {
            TenantId = tid,
            SourceType = sourceType,
            Title = title,
            Language = language,
            FileUrl = fileUrl,
            FileName = fileName,
            MimeType = mimeType,
            SourceUrl = sourceUrl,
            Status = KnowledgeDocumentStatus.Pending
        };
        db.KnowledgeDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(document.Id);
    }

    // ────────────────────────────────────────────────── helpers ──

    private async Task RemoveDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        if (document.Chunks.Count > 0)
        {
            await vectorStore.DeleteAsync(document.Chunks.Select(c => c.QdrantPointId).ToList(), cancellationToken);
            db.KnowledgeVectors.RemoveRange(document.Chunks);
        }
        db.KnowledgeDocuments.Remove(document);
    }

    private static Language ParseLanguage(string value) =>
        Enum.TryParse<Language>(value, ignoreCase: true, out var lang) ? lang : Language.English;
}
