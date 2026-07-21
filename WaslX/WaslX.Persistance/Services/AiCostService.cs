using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Cross-tenant AI cost monitoring + budget-alert CRUD (US-6.5). Deliberately CROSS-TENANT — every
/// aggregation groups over all tenants with no tenant filter, because the only caller is the SuperAdmin
/// console guarded by <c>[Authorize(Roles = "SuperAdmin")]</c>. The cost read is fully graceful: when the
/// AI pipeline (a separate sprint) has written no <see cref="AiUsageRecord"/> rows, everything is zero /
/// empty rather than an error. Every alert mutation is written to the platform audit trail.
/// </summary>
internal sealed class AiCostService(
    ApplicationDbContext db,
    IPlatformAuditService audit) : IAiCostService
{
    private const int DefaultRangeDays = 30;

    public async Task<Result<AiCostResponse>> GetCostAsync(AiCostQuery query, CancellationToken cancellationToken = default)
    {
        var to = (query.To ?? DateTime.UtcNow).Date;
        var from = (query.From ?? to.AddDays(-(DefaultRangeDays - 1))).Date;
        if (from > to) (from, to) = (to, from);
        var toExclusive = to.AddDays(1);

        // Platform-wide totals over ALL time (no tenant filter). Sum over an empty set is 0m / 0.
        var totalCost = await db.AiUsageRecords.AsNoTracking().SumAsync(r => (decimal?)r.CostUsd, cancellationToken) ?? 0m;
        var totalTokens = await db.AiUsageRecords.AsNoTracking().SumAsync(r => (long?)r.TotalTokens, cancellationToken) ?? 0L;

        // Per-tenant breakdown. EF Core cannot translate a GroupBy result Join-ed back to another DbSet,
        // so aggregate server-side first, then resolve tenant names via a lookup and join in memory.
        var perTenantRaw = await db.AiUsageRecords.AsNoTracking()
            .GroupBy(r => r.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                CostUsd = g.Sum(r => r.CostUsd),
                Tokens = g.Sum(r => (long)r.TotalTokens)
            })
            .ToListAsync(cancellationToken);

        var tenantNames = await db.Tenants.AsNoTracking()
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var perTenant = perTenantRaw
            .Select(x => new TenantAiCostRow(
                x.TenantId,
                tenantNames.TryGetValue(x.TenantId, out var name) ? name : $"Tenant {x.TenantId}",
                x.CostUsd,
                x.Tokens))
            .OrderByDescending(x => x.CostUsd)
            .ToList();

        var byModelRaw = await db.AiUsageRecords.AsNoTracking()
            .GroupBy(r => r.Model)
            .Select(g => new { Model = g.Key, CostUsd = g.Sum(r => r.CostUsd), Tokens = g.Sum(r => (long)r.TotalTokens) })
            .ToListAsync(cancellationToken);
        var byModel = byModelRaw
            .Select(x => new AiCostByModel(x.Model, x.CostUsd, x.Tokens))
            .OrderByDescending(x => x.CostUsd)
            .ToList();

        var byComponentRaw = await db.AiUsageRecords.AsNoTracking()
            .GroupBy(r => r.Component)
            .Select(g => new { Component = g.Key, CostUsd = g.Sum(r => r.CostUsd), Tokens = g.Sum(r => (long)r.TotalTokens) })
            .ToListAsync(cancellationToken);
        var byComponent = byComponentRaw
            .Select(x => new AiCostByComponent(x.Component, x.CostUsd, x.Tokens))
            .OrderByDescending(x => x.CostUsd)
            .ToList();

        // Daily series over the window (dense — zero-filled). Grouped in memory over the date-bounded rows
        // to avoid EF GroupBy-projection translation limits.
        var dailyRows = await db.AiUsageRecords.AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt < toExclusive)
            .Select(r => new { r.CreatedAt, r.CostUsd, r.TotalTokens })
            .ToListAsync(cancellationToken);
        var dailyRaw = dailyRows
            .GroupBy(r => r.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => new { CostUsd = g.Sum(r => r.CostUsd), Tokens = g.Sum(r => (long)r.TotalTokens) });

        var daily = new List<AiCostDailyPoint>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (dailyRaw.TryGetValue(day, out var point))
                daily.Add(new AiCostDailyPoint(day, point.CostUsd, point.Tokens));
            else
                daily.Add(new AiCostDailyPoint(day, 0m, 0L));
        }

        var breaches = await ComputeBreachesAsync(cancellationToken);

        return Result.Success(new AiCostResponse(
            totalCost, totalTokens, perTenant, byModel, byComponent, daily, breaches));
    }

    /// <summary>
    /// For each active budget alert, sum the accrued AI cost over its rolling period + scope and flag it
    /// as breached when the accrued spend meets or exceeds the threshold.
    /// </summary>
    private async Task<IReadOnlyList<BudgetBreach>> ComputeBreachesAsync(CancellationToken cancellationToken)
    {
        var alerts = await db.BudgetAlerts.AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);
        if (alerts.Count == 0) return [];

        var now = DateTime.UtcNow;
        var breaches = new List<BudgetBreach>();

        foreach (var alert in alerts)
        {
            var windowStart = PeriodStart(alert.Period, now);

            var q = db.AiUsageRecords.AsNoTracking().Where(r => r.CreatedAt >= windowStart);
            if (alert.TenantId is { } tenantId)
                q = q.Where(r => r.TenantId == tenantId);

            var actual = await q.SumAsync(r => (decimal?)r.CostUsd, cancellationToken) ?? 0m;
            if (actual >= alert.ThresholdUsd && alert.ThresholdUsd > 0m)
                breaches.Add(new BudgetBreach(alert.Id, alert.TenantId, alert.Scope, alert.Period, alert.ThresholdUsd, actual));
        }

        return breaches;
    }

    // Rolling window start for a period keyword. Unknown/blank periods fall back to a monthly window.
    private static DateTime PeriodStart(string period, DateTime now) =>
        (period ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "daily" or "day" => now.Date,
            "weekly" or "week" => now.Date.AddDays(-7),
            _ => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    // ── Budget-alert CRUD ──

    public async Task<Result<IReadOnlyList<BudgetAlertResponse>>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await db.BudgetAlerts.AsNoTracking()
            .OrderBy(a => a.TenantId == null ? 0 : 1).ThenBy(a => a.TenantId).ThenBy(a => a.Id)
            .Select(a => Map(a))
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<BudgetAlertResponse>>(alerts);
    }

    public async Task<Result<BudgetAlertResponse>> CreateAlertAsync(CreateBudgetAlertInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        if (input.ThresholdUsd <= 0m)
            return Result.Failure<BudgetAlertResponse>(AppErrors.BudgetThresholdInvalid);

        if (input.TenantId is { } tenantId && !await db.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken))
            return Result.Failure<BudgetAlertResponse>(AppErrors.TenantNotFound);

        var alert = new BudgetAlert
        {
            TenantId = input.TenantId,
            Scope = (input.Scope ?? string.Empty).Trim(),
            ThresholdUsd = input.ThresholdUsd,
            Period = string.IsNullOrWhiteSpace(input.Period) ? "Monthly" : input.Period.Trim(),
            IsActive = input.IsActive,
            NotifyEmail = Clean(input.NotifyEmail)
        };
        db.BudgetAlerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "BudgetAlertCreated", "BudgetAlert", alert.Id.ToString(),
            alert.TenantId, $"scope={alert.Scope}; threshold={alert.ThresholdUsd}; period={alert.Period}", actor.Ip, cancellationToken);

        return Result.Success(Map(alert));
    }

    public async Task<Result<BudgetAlertResponse>> UpdateAlertAsync(int id, UpdateBudgetAlertInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var alert = await db.BudgetAlerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (alert is null) return Result.Failure<BudgetAlertResponse>(AppErrors.BudgetAlertNotFound);

        if (input.ThresholdUsd <= 0m)
            return Result.Failure<BudgetAlertResponse>(AppErrors.BudgetThresholdInvalid);

        alert.Scope = (input.Scope ?? string.Empty).Trim();
        alert.ThresholdUsd = input.ThresholdUsd;
        alert.Period = string.IsNullOrWhiteSpace(input.Period) ? "Monthly" : input.Period.Trim();
        alert.IsActive = input.IsActive;
        alert.NotifyEmail = Clean(input.NotifyEmail);
        alert.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "BudgetAlertUpdated", "BudgetAlert", alert.Id.ToString(),
            alert.TenantId, $"scope={alert.Scope}; threshold={alert.ThresholdUsd}; period={alert.Period}; active={alert.IsActive}", actor.Ip, cancellationToken);

        return Result.Success(Map(alert));
    }

    public async Task<Result> DeleteAlertAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var alert = await db.BudgetAlerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (alert is null) return Result.Failure(AppErrors.BudgetAlertNotFound);

        db.BudgetAlerts.Remove(alert);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "BudgetAlertDeleted", "BudgetAlert", id.ToString(),
            alert.TenantId, $"scope={alert.Scope}", actor.Ip, cancellationToken);

        return Result.Success();
    }

    private static BudgetAlertResponse Map(BudgetAlert a) =>
        new(a.Id, a.TenantId, a.Scope, a.ThresholdUsd, a.Period, a.IsActive, a.LastTriggeredAt, a.NotifyEmail, a.CreatedAt);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
