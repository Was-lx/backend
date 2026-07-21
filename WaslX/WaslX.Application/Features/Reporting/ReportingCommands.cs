using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Reporting;
using WaslX.Application.Features.Reporting.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Reporting;

// All reporting requests carry the caller's tenant, role and domain user id so the service can
// enforce tenant isolation and role-scoping (Agent → own metrics only), plus an optional TeamId
// (Group) filter. These are read-only queries.

public record GetOverviewQuery(int? TenantId, string? Role, int? DomainUserId, int? TeamId, DateTime From, DateTime To) : IQuery<OverviewResponse>;
public class GetOverviewQueryHandler(IReportingService svc) : IQueryHandler<GetOverviewQuery, OverviewResponse>
{
    public Task<Result<OverviewResponse>> Handle(GetOverviewQuery request, CancellationToken cancellationToken) =>
        svc.GetOverviewAsync(request.TenantId, request.Role, request.DomainUserId, request.TeamId, request.From, request.To, cancellationToken);
}

public record GetAgentPerformanceQuery(int? TenantId, string? Role, int? DomainUserId, int? TeamId, DateTime From, DateTime To) : IQuery<AgentReportResponse>;
public class GetAgentPerformanceQueryHandler(IReportingService svc) : IQueryHandler<GetAgentPerformanceQuery, AgentReportResponse>
{
    public Task<Result<AgentReportResponse>> Handle(GetAgentPerformanceQuery request, CancellationToken cancellationToken) =>
        svc.GetAgentPerformanceAsync(request.TenantId, request.Role, request.DomainUserId, request.TeamId, request.From, request.To, cancellationToken);
}

public record GetResponseTimesQuery(int? TenantId, string? Role, int? DomainUserId, int? TeamId, DateTime From, DateTime To) : IQuery<ResponseTimesResponse>;
public class GetResponseTimesQueryHandler(IReportingService svc) : IQueryHandler<GetResponseTimesQuery, ResponseTimesResponse>
{
    public Task<Result<ResponseTimesResponse>> Handle(GetResponseTimesQuery request, CancellationToken cancellationToken) =>
        svc.GetResponseTimesAsync(request.TenantId, request.Role, request.DomainUserId, request.TeamId, request.From, request.To, cancellationToken);
}

public record GetConversationVolumeQuery(int? TenantId, string? Role, int? DomainUserId, int? TeamId, DateTime From, DateTime To) : IQuery<ConversationVolumeResponse>;
public class GetConversationVolumeQueryHandler(IReportingService svc) : IQueryHandler<GetConversationVolumeQuery, ConversationVolumeResponse>
{
    public Task<Result<ConversationVolumeResponse>> Handle(GetConversationVolumeQuery request, CancellationToken cancellationToken) =>
        svc.GetConversationVolumeAsync(request.TenantId, request.Role, request.DomainUserId, request.TeamId, request.From, request.To, cancellationToken);
}

public record GetRoutingStatsQuery(int? TenantId, string? Role, int? DomainUserId, int? TeamId, DateTime From, DateTime To) : IQuery<RoutingStatsResponse>;
public class GetRoutingStatsQueryHandler(IReportingService svc) : IQueryHandler<GetRoutingStatsQuery, RoutingStatsResponse>
{
    public Task<Result<RoutingStatsResponse>> Handle(GetRoutingStatsQuery request, CancellationToken cancellationToken) =>
        svc.GetRoutingStatsAsync(request.TenantId, request.Role, request.DomainUserId, request.TeamId, request.From, request.To, cancellationToken);
}

public record ExportReportQuery(int? TenantId, string? Role, int? DomainUserId, int? TeamId, string ReportKey, string Format, DateTime From, DateTime To) : IQuery<ReportFile>;
public class ExportReportQueryHandler(IReportingService svc) : IQueryHandler<ExportReportQuery, ReportFile>
{
    public Task<Result<ReportFile>> Handle(ExportReportQuery request, CancellationToken cancellationToken) =>
        svc.ExportAsync(request.TenantId, request.Role, request.DomainUserId, request.TeamId, request.ReportKey, request.Format, request.From, request.To, cancellationToken);
}
