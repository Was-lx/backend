using WaslX.Application.Features.Plans.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Billing;

public interface ISubscriptionPlanService
{
    /// <summary>All plans — for the SuperAdmin console.</summary>
    Task<Result<IReadOnlyList<PlanResponse>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Active, public plans — for the landing pricing page & the in-app upgrade screen.</summary>
    Task<Result<IReadOnlyList<PlanResponse>>> GetPublicAsync(CancellationToken cancellationToken = default);

    Task<Result<PlanResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<PlanResponse>> CreateAsync(UpsertPlanRequest request, CancellationToken cancellationToken = default);

    Task<Result<PlanResponse>> UpdateAsync(int id, UpsertPlanRequest request, CancellationToken cancellationToken = default);

    Task<Result> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
