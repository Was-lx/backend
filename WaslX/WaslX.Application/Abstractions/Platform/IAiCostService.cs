using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Cross-tenant AI cost monitoring + budget-alert management (US-6.5 — Platform Owner console).
/// Deliberately NOT tenant-scoped: every aggregation groups over all tenants and is only reached through
/// the SuperAdmin console guarded by <c>[Authorize(Roles = "SuperAdmin")]</c>. The cost read is fully
/// graceful — if the AI pipeline (a later sprint) has written no <c>AiUsageRecord</c> rows, it returns
/// zeros/empties rather than throwing. Every budget-alert mutation is written to the platform audit trail.
/// </summary>
public interface IAiCostService
{
    Task<Result<AiCostResponse>> GetCostAsync(AiCostQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<BudgetAlertResponse>>> GetAlertsAsync(CancellationToken cancellationToken = default);
    Task<Result<BudgetAlertResponse>> CreateAlertAsync(CreateBudgetAlertInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<BudgetAlertResponse>> UpdateAlertAsync(int id, UpdateBudgetAlertInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result> DeleteAlertAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default);
}
