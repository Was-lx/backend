using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Features.Tenants.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class TenantService(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : ITenantService
{
    public async Task<Result<TenantProfileResponse>> GetProfileAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.AsNoTracking().Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure<TenantProfileResponse>(AppErrors.TenantNotFound);
        return Result.Success(Map(tenant));
    }

    public async Task<Result> UpdateProfileAsync(int tenantId, UpdateTenantProfileInput input, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        tenant.Name = input.Name.Trim();
        tenant.Website = Clean(input.Website);
        tenant.Industry = Clean(input.Industry);
        tenant.PhoneNumber = Clean(input.Phone);
        tenant.CustomerType = Enum.TryParse<CustomerType>(input.CustomerType, true, out var ct) ? ct : CustomerType.Unknown;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateOnboardingAsync(int tenantId, int step, bool completed, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        if (step < 0) return Result.Failure(AppErrors.OnboardingInvalidStep);

        tenant.OnboardingStep = step;
        tenant.OnboardingCompleted = completed;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<TenantSummaryResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await db.Tenants.AsNoTracking().Include(t => t.Plan)
            .OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);

        var stats = await userManager.Users.Where(u => u.TenantId != null)
            .GroupBy(u => u.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, Count = g.Count(), FirstEmail = g.Min(u => u.Email) })
            .ToListAsync(cancellationToken);
        var statsById = stats.ToDictionary(s => s.TenantId);

        var list = tenants.Select(t =>
        {
            statsById.TryGetValue(t.Id, out var s);
            return new TenantSummaryResponse(
                t.Id, t.Name, t.Plan?.Name ?? "", t.Status.ToString(), t.BillingStatus.ToString(),
                t.TrialEndsAt, s?.Count ?? 0, s?.FirstEmail, t.CreatedAt);
        }).ToList();

        return Result.Success<IReadOnlyList<TenantSummaryResponse>>(list);
    }

    public async Task<Result> SetStatusAsync(int tenantId, string status, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        if (!Enum.TryParse<TenantStatus>(status, true, out var parsed))
            return Result.Failure(new Error("Tenant.InvalidStatus", "Invalid tenant status", 400));

        tenant.Status = parsed;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static TenantProfileResponse Map(Domain.Entities.Tenant t)
    {
        int? trialDaysLeft = t.TrialEndsAt is { } end
            ? Math.Max(0, (int)Math.Ceiling((end - DateTime.UtcNow).TotalDays))
            : null;

        return new TenantProfileResponse(
            t.Id, t.Name, t.Website, t.Industry, t.PhoneNumber, t.CustomerType.ToString(),
            t.Status.ToString(), t.BillingStatus.ToString(), t.PlanId, t.Plan?.Name ?? "",
            t.TrialEndsAt, trialDaysLeft, t.CurrentPeriodEnd, t.OnboardingStep, t.OnboardingCompleted);
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
