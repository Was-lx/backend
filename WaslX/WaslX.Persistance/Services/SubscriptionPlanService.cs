using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Features.Plans.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class SubscriptionPlanService(ApplicationDbContext db) : ISubscriptionPlanService
{
    public async Task<Result<IReadOnlyList<PlanResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await db.SubscriptionPlans.AsNoTracking()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Price)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlanResponse>>(plans.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<PlanResponse>>> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var plans = await db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive && p.IsPublic)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Price)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlanResponse>>(plans.Select(Map).ToList());
    }

    public async Task<Result<PlanResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var plan = await db.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return plan is null
            ? Result.Failure<PlanResponse>(AppErrors.PlanNotFound)
            : Result.Success(Map(plan));
    }

    public async Task<Result<PlanResponse>> CreateAsync(UpsertPlanRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await db.SubscriptionPlans.AnyAsync(p => p.Code == code, cancellationToken))
            return Result.Failure<PlanResponse>(AppErrors.DuplicatePlanCode);

        var plan = new SubscriptionPlan();
        Apply(plan, request, code);
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(plan));
    }

    public async Task<Result<PlanResponse>> UpdateAsync(int id, UpsertPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null) return Result.Failure<PlanResponse>(AppErrors.PlanNotFound);

        var code = request.Code.Trim().ToLowerInvariant();
        if (code != plan.Code && await db.SubscriptionPlans.AnyAsync(p => p.Code == code && p.Id != id, cancellationToken))
            return Result.Failure<PlanResponse>(AppErrors.DuplicatePlanCode);

        Apply(plan, request, code);
        plan.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(plan));
    }

    public async Task<Result> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null) return Result.Failure(AppErrors.PlanNotFound);

        plan.IsActive = isActive;
        plan.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void Apply(SubscriptionPlan plan, UpsertPlanRequest r, string code)
    {
        plan.Code = code;
        plan.Name = r.Name.Trim();
        plan.Tagline = r.Tagline;
        plan.Price = r.Price;
        plan.PriceYearly = r.PriceYearly;
        plan.BillingCycle = Enum.TryParse<BillingCycle>(r.BillingCycle, true, out var bc) ? bc : BillingCycle.Monthly;
        plan.MaxAgents = r.MaxAgents;
        plan.MaxNumbers = r.MaxNumbers;
        plan.MsgQuota = r.MsgQuota;
        plan.AiQuota = r.AiQuota;
        plan.TrialDays = r.TrialDays;
        plan.IsActive = r.IsActive;
        plan.IsPublic = r.IsPublic;
        plan.IsCustom = r.IsCustom;
        plan.SortOrder = r.SortOrder;
        plan.Features = r.Features ?? new List<string>();
    }

    private static PlanResponse Map(SubscriptionPlan p) => new(
        p.Id, p.Code, p.Name, p.Tagline, p.Price, p.PriceYearly, p.BillingCycle.ToString(),
        p.MaxAgents, p.MaxNumbers, p.MsgQuota, p.AiQuota, p.TrialDays,
        p.IsActive, p.IsPublic, p.IsCustom, p.SortOrder, p.Features);
}
