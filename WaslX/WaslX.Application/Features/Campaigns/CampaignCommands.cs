using WaslX.Application.Abstractions.Campaigns;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Campaigns.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Campaigns;

// ── Queries ──
public record GetCampaignsQuery(int? TenantId) : IQuery<IReadOnlyList<CampaignResponse>>;
public class GetCampaignsQueryHandler(ICampaignService svc) : IQueryHandler<GetCampaignsQuery, IReadOnlyList<CampaignResponse>>
{
    public Task<Result<IReadOnlyList<CampaignResponse>>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(request.TenantId, cancellationToken);
}

public record GetCampaignByIdQuery(int? TenantId, int Id) : IQuery<CampaignDetailResponse>;
public class GetCampaignByIdQueryHandler(ICampaignService svc) : IQueryHandler<GetCampaignByIdQuery, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken) =>
        svc.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
}

public record GetCampaignRecipientsQuery(int? TenantId, int Id) : IQuery<IReadOnlyList<CampaignRecipientResponse>>;
public class GetCampaignRecipientsQueryHandler(ICampaignService svc) : IQueryHandler<GetCampaignRecipientsQuery, IReadOnlyList<CampaignRecipientResponse>>
{
    public Task<Result<IReadOnlyList<CampaignRecipientResponse>>> Handle(GetCampaignRecipientsQuery request, CancellationToken cancellationToken) =>
        svc.GetRecipientsAsync(request.TenantId, request.Id, cancellationToken);
}

// ── Commands ──
public record PreviewAudienceCommand(int? TenantId, AudiencePreviewRequest Request) : ICommand<AudiencePreviewResponse>;
public class PreviewAudienceCommandHandler(ICampaignService svc) : ICommandHandler<PreviewAudienceCommand, AudiencePreviewResponse>
{
    public Task<Result<AudiencePreviewResponse>> Handle(PreviewAudienceCommand request, CancellationToken cancellationToken) =>
        svc.PreviewAudienceAsync(request.TenantId, request.Request, cancellationToken);
}

public record GetAudienceContactsQuery(int? TenantId, int? TagId, DateTime? DateFrom, DateTime? DateTo) : IQuery<IReadOnlyList<AudienceContactDto>>;
public class GetAudienceContactsQueryHandler(ICampaignService svc) : IQueryHandler<GetAudienceContactsQuery, IReadOnlyList<AudienceContactDto>>
{
    public Task<Result<IReadOnlyList<AudienceContactDto>>> Handle(GetAudienceContactsQuery request, CancellationToken cancellationToken) =>
        svc.GetAudienceContactsAsync(request.TenantId, request.TagId, request.DateFrom, request.DateTo, cancellationToken);
}

public record CreateCampaignCommand(int? TenantId, int? CreatedByDomainUserId, CreateCampaignRequest Request) : ICommand<CampaignDetailResponse>;
public class CreateCampaignCommandHandler(ICampaignService svc) : ICommandHandler<CreateCampaignCommand, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(CreateCampaignCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.TenantId, request.CreatedByDomainUserId, request.Request, cancellationToken);
}

public record UpdateCampaignCommand(int? TenantId, int Id, UpdateCampaignRequest Request) : ICommand<CampaignDetailResponse>;
public class UpdateCampaignCommandHandler(ICampaignService svc) : ICommandHandler<UpdateCampaignCommand, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAsync(request.TenantId, request.Id, request.Request, cancellationToken);
}

public record DeleteCampaignCommand(int? TenantId, int Id) : ICommand;
public class DeleteCampaignCommandHandler(ICampaignService svc) : ICommandHandler<DeleteCampaignCommand>
{
    public Task<Result> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAsync(request.TenantId, request.Id, cancellationToken);
}

public record LaunchCampaignCommand(int? TenantId, int Id) : ICommand<CampaignDetailResponse>;
public class LaunchCampaignCommandHandler(ICampaignService svc) : ICommandHandler<LaunchCampaignCommand, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(LaunchCampaignCommand request, CancellationToken cancellationToken) =>
        svc.LaunchAsync(request.TenantId, request.Id, cancellationToken);
}

public record PauseCampaignCommand(int? TenantId, int Id) : ICommand<CampaignDetailResponse>;
public class PauseCampaignCommandHandler(ICampaignService svc) : ICommandHandler<PauseCampaignCommand, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(PauseCampaignCommand request, CancellationToken cancellationToken) =>
        svc.PauseAsync(request.TenantId, request.Id, cancellationToken);
}

public record ResumeCampaignCommand(int? TenantId, int Id) : ICommand<CampaignDetailResponse>;
public class ResumeCampaignCommandHandler(ICampaignService svc) : ICommandHandler<ResumeCampaignCommand, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(ResumeCampaignCommand request, CancellationToken cancellationToken) =>
        svc.ResumeAsync(request.TenantId, request.Id, cancellationToken);
}

public record CancelCampaignCommand(int? TenantId, int Id) : ICommand<CampaignDetailResponse>;
public class CancelCampaignCommandHandler(ICampaignService svc) : ICommandHandler<CancelCampaignCommand, CampaignDetailResponse>
{
    public Task<Result<CampaignDetailResponse>> Handle(CancelCampaignCommand request, CancellationToken cancellationToken) =>
        svc.CancelAsync(request.TenantId, request.Id, cancellationToken);
}
