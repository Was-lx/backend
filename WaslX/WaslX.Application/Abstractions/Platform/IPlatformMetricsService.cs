using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Cross-tenant platform usage metrics (US-6.4 — Platform Owner console). Deliberately NOT tenant-scoped:
/// it groups by <c>TenantId</c> across every tenant and is only ever reached through the SuperAdmin
/// console guarded by <c>[Authorize(Roles = "SuperAdmin")]</c>. Read-only — no audit surface.
/// </summary>
public interface IPlatformMetricsService
{
    Task<Result<PlatformUsageResponse>> GetUsageAsync(PlatformUsageQuery query, CancellationToken cancellationToken = default);
}
