using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Cross-tenant platform usage metrics (US-6.4). Deliberately CROSS-TENANT — every query groups by
/// <c>TenantId</c> with no tenant filter, because the only caller is the SuperAdmin console guarded by
/// <c>[Authorize(Roles = "SuperAdmin")]</c>. Read-only; nothing is mutated so there is no audit call.
/// </summary>
internal sealed class PlatformMetricsService(ApplicationDbContext db) : IPlatformMetricsService
{
    private const int DefaultRangeDays = 30;

    public async Task<Result<PlatformUsageResponse>> GetUsageAsync(PlatformUsageQuery query, CancellationToken cancellationToken = default)
    {
        // Daily-series window (inclusive). Default to the trailing 30 days.
        var to = (query.To ?? DateTime.UtcNow).Date;
        var from = (query.From ?? to.AddDays(-(DefaultRangeDays - 1))).Date;
        if (from > to) (from, to) = (to, from);
        var toExclusive = to.AddDays(1);

        // ── Tenant headline counts ──
        var totalTenants = await db.Tenants.AsNoTracking().CountAsync(cancellationToken);
        var activeTenants = await db.Tenants.AsNoTracking()
            .CountAsync(t => t.Status == TenantStatus.Active, cancellationToken);

        var totalConversations = await db.Conversations.AsNoTracking().LongCountAsync(cancellationToken);
        var totalMessages = await db.Messages.AsNoTracking().LongCountAsync(cancellationToken);
        var activeAgents = await db.Users.AsNoTracking().CountAsync(u => u.Status == "Active", cancellationToken);

        // ── Per-tenant breakdown (grouped by TenantId — no tenant filter) ──
        var convByTenant = await db.Conversations.AsNoTracking()
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var msgByTenant = await db.Messages.AsNoTracking()
            .GroupBy(m => m.Conversation.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var agentsByTenant = await db.Users.AsNoTracking()
            .Where(u => u.Status == "Active")
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var tenants = await db.Tenants.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.Status })
            .ToListAsync(cancellationToken);

        var perTenant = tenants
            .Select(t => new TenantUsageRow(
                t.Id,
                t.Name,
                t.Status.ToString(),
                convByTenant.GetValueOrDefault(t.Id),
                msgByTenant.GetValueOrDefault(t.Id),
                agentsByTenant.GetValueOrDefault(t.Id)))
            .ToList();

        // ── Daily time series over the window ──
        var convDaily = await db.Conversations.AsNoTracking()
            .Where(c => c.CreatedAt >= from && c.CreatedAt < toExclusive)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        var msgDaily = await db.Messages.AsNoTracking()
            .Where(m => m.Timestamp >= from && m.Timestamp < toExclusive)
            .GroupBy(m => m.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        var daily = new List<UsageDailyPoint>();
        for (var day = from; day <= to; day = day.AddDays(1))
            daily.Add(new UsageDailyPoint(day, convDaily.GetValueOrDefault(day), msgDaily.GetValueOrDefault(day)));

        return Result.Success(new PlatformUsageResponse(
            activeTenants, totalTenants, totalConversations, totalMessages, activeAgents, perTenant, daily));
    }
}
