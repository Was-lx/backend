using WaslX.Application.Features.Campaigns.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Campaigns;

/// <summary>
/// Outbound WhatsApp broadcast campaigns (FR-CMP), tenant-scoped. Owns the campaign lifecycle
/// (Draft → Scheduled/Running → Paused/Completed/Cancelled), audience resolution and the
/// materialisation of per-recipient delivery rows handed to the Hangfire send engine.
/// </summary>
public interface ICampaignService
{
    Task<Result<IReadOnlyList<CampaignResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> GetByIdAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> CreateAsync(int? tenantId, int? createdByDomainUserId, CreateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> UpdateAsync(int? tenantId, int id, UpdateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<AudiencePreviewResponse>> PreviewAudienceAsync(int? tenantId, AudiencePreviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AudienceContactDto>>> GetAudienceContactsAsync(int? tenantId, int? tagId, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> LaunchAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> PauseAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> ResumeAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<CampaignDetailResponse>> CancelAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CampaignRecipientResponse>>> GetRecipientsAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
}
