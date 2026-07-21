using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Application.Features.Tenants.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class TenantService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPlatformAuditService audit) : ITenantService
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

    public async Task<Result<TenantDetailResponse>> GetDetailAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.AsNoTracking().Include(t => t.Plan)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure<TenantDetailResponse>(AppErrors.TenantNotFound);

        var appUsers = await userManager.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
        var waCount = await db.WhatsAppAccounts.CountAsync(w => w.TenantId == tenantId, cancellationToken);

        // Workspace users (Users tab): each user's primary role comes from Identity.
        var userRows = new List<TenantUserRowDto>(appUsers.Count);
        foreach (var u in appUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            userRows.Add(new TenantUserRowDto(
                u.Id, u.FullName, u.Email ?? string.Empty, roles.FirstOrDefault() ?? string.Empty, !u.IsDisabled));
        }

        // Invoices tab: newest first. Status is materialised before ToString() to avoid enum translation.
        var invoiceRows = await db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => new { i.Id, i.TenantId, i.Amount, i.Status, i.IssuedAt, i.DueDate, i.PaidAt })
            .ToListAsync(cancellationToken);
        var invoices = invoiceRows
            .Select(i => new TenantInvoiceDto(
                i.Id, i.TenantId, tenant.Name, i.Amount, i.Status.ToString(),
                null, null, i.IssuedAt, i.DueDate, i.PaidAt))
            .ToList();

        int? trialDaysLeft = tenant.TrialEndsAt is { } end
            ? Math.Max(0, (int)Math.Ceiling((end - DateTime.UtcNow).TotalDays))
            : null;

        var plan = tenant.Plan;
        var price = plan?.Price ?? 0m;
        var usage = new TenantUsageDto(
            appUsers.Count, plan?.MaxAgents ?? 0, waCount, plan?.MaxNumbers ?? 0,
            plan?.MsgQuota ?? 0, plan?.AiQuota ?? 0);
        var billingCycle = tenant.SelectedBillingCycle?.ToString() ?? "Monthly";

        var detail = new TenantDetailResponse(
            tenant.Id, tenant.Name, tenant.Website, tenant.Industry, tenant.PhoneNumber, tenant.CustomerType.ToString(),
            tenant.Status.ToString(), tenant.BillingStatus.ToString(),
            tenant.PlanId, plan?.Name ?? "", price, billingCycle,
            tenant.TrialEndsAt, trialDaysLeft, tenant.CurrentPeriodEnd, tenant.CreatedAt,
            usage, price, userRows, invoices);

        return Result.Success(detail);
    }

    public async Task<Result> ConfigureAsync(int tenantId, ConfigureTenantInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == input.PlanId, cancellationToken);
        if (plan is null) return Result.Failure(AppErrors.PlanNotFound);

        var previousPlanId = tenant.PlanId;

        tenant.Name = input.Name.Trim();
        tenant.Website = Clean(input.Website);
        tenant.Industry = Clean(input.Industry);
        tenant.PhoneNumber = Clean(input.Phone);
        tenant.CustomerType = Enum.TryParse<CustomerType>(input.CustomerType, true, out var ct) ? ct : CustomerType.Unknown;
        tenant.PlanId = plan.Id;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "TenantConfigured", "Tenant", tenant.Id.ToString(),
            tenant.Id, $"name='{tenant.Name}'; plan {previousPlanId}→{plan.Id}", actor.Ip, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetStatusAsync(int tenantId, string status, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        if (!Enum.TryParse<TenantStatus>(status, true, out var parsed))
            return Result.Failure(new Error("Tenant.InvalidStatus", "Invalid tenant status", 400));

        var previous = tenant.Status;
        tenant.Status = parsed;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "TenantStatusChanged", "Tenant", tenant.Id.ToString(),
            tenant.Id, $"{previous}→{parsed}", actor.Ip, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SoftDeleteAsync(int tenantId, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        // Soft-delete / deactivate: mark Cancelled (and stop billing renewal). Data is retained;
        // sign-in is already blocked for a Cancelled workspace by the auth flow.
        var previous = tenant.Status;
        tenant.Status = TenantStatus.Cancelled;
        tenant.BillingStatus = BillingStatus.Cancelled;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "TenantDeactivated", "Tenant", tenant.Id.ToString(),
            tenant.Id, $"status {previous}→Cancelled (soft-delete)", actor.Ip, cancellationToken);

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
