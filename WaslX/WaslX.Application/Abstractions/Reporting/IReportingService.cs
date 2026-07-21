using WaslX.Application.Features.Reporting.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Reporting;

/// <summary>
/// Read-only reporting &amp; analytics aggregation (tenant-scoped). Role-scoped: Admin/Manager see the
/// whole workspace, an Agent sees only their own conversations/metrics. Metrics that depend on
/// AI-pipeline data (routing decisions) degrade gracefully to zero/empty when not yet populated.
/// </summary>
public interface IReportingService
{
    Task<Result<OverviewResponse>> GetOverviewAsync(int? tenantId, string? role, int? domainUserId, int? teamId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Result<AgentReportResponse>> GetAgentPerformanceAsync(int? tenantId, string? role, int? domainUserId, int? teamId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Result<ResponseTimesResponse>> GetResponseTimesAsync(int? tenantId, string? role, int? domainUserId, int? teamId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Result<ConversationVolumeResponse>> GetConversationVolumeAsync(int? tenantId, string? role, int? domainUserId, int? teamId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Result<RoutingStatsResponse>> GetRoutingStatsAsync(int? tenantId, string? role, int? domainUserId, int? teamId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Result<ReportFile>> ExportAsync(int? tenantId, string? role, int? domainUserId, int? teamId, string reportKey, string format, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
