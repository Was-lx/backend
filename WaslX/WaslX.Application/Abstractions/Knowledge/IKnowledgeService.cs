using WaslX.Application.Features.Knowledge.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Abstractions.Knowledge;

/// <summary>
/// Tenant-scoped knowledge-base management: FAQ CRUD, document/website ingest-unit lifecycle
/// (list, delete, reindex). Every mutation that needs (re-)indexing returns the touched
/// <see cref="Domain.Entities.KnowledgeDocument"/> id so the caller (API layer) can enqueue the
/// Hangfire ingestion job — this service never touches Hangfire directly (Application has no
/// dependency on it).
/// </summary>
public interface IKnowledgeService
{
    Task<Result<KnowledgePagedResult<FaqResponse>>> GetFaqsAsync(int? tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<KnowledgeMutationResult>> CreateFaqAsync(int? tenantId, UpsertFaqRequest request, CancellationToken cancellationToken = default);
    Task<Result<KnowledgeMutationResult>> UpdateFaqAsync(int? tenantId, int faqId, UpsertFaqRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteFaqAsync(int? tenantId, int faqId, CancellationToken cancellationToken = default);

    Task<Result<KnowledgePagedResult<KnowledgeDocumentResponse>>> GetDocumentsAsync(
        int? tenantId, KnowledgeSourceType? sourceType, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> DeleteDocumentAsync(int? tenantId, int documentId, CancellationToken cancellationToken = default);

    /// <summary>Validates the document belongs to the tenant and resets it to Pending; returns its id for enqueue.</summary>
    Task<Result<int>> PrepareReindexAsync(int? tenantId, int documentId, CancellationToken cancellationToken = default);

    /// <summary>Creates a Pending document row for a newly uploaded file/website and returns its id for enqueue.</summary>
    Task<Result<int>> CreateDocumentAsync(
        int? tenantId, KnowledgeSourceType sourceType, string title, Language language,
        string? fileUrl = null, string? fileName = null, string? mimeType = null, string? sourceUrl = null,
        CancellationToken cancellationToken = default);
}
