using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaslX.Application.Abstractions.Distribution;
using WaslX.Application.Abstractions.Maintenance;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Recurring distribution-engine maintenance (Sprint 3, Phase B). Runs on Hangfire's schedule:
/// auto-resolve of stale conversations, and reassignment of open work away from offline agents.
/// Both handlers are defensive — a failure on one tenant/agent is logged and skipped, never rethrown.
/// </summary>
internal sealed class MaintenanceJobs(
    ApplicationDbContext db,
    IDistributionService distribution,
    ILogger<MaintenanceJobs> logger) : IMaintenanceJobs
{
    // An agent whose last heartbeat is older than this (and not flagged online) is treated as offline.
    private static readonly TimeSpan OfflineGrace = TimeSpan.FromMinutes(30);

    public async Task AutoResolveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Per-tenant because the stale window (AutoResolveHours) is tenant-specific.
        var tenants = await db.Tenants.AsNoTracking()
            .Where(t => t.AutoResolveEnabled)
            .Select(t => new { t.Id, t.AutoResolveHours })
            .ToListAsync(cancellationToken);

        var totalResolved = 0;
        foreach (var tenant in tenants)
        {
            var cutoff = now.AddHours(-tenant.AutoResolveHours);

            var stale = await db.Conversations
                .Where(c => c.TenantId == tenant.Id
                    && c.Status != ConversationStatus.Resolved
                    && !c.IsDeleted
                    && c.LastMessageAt < cutoff)
                .ToListAsync(cancellationToken);

            if (stale.Count == 0)
                continue;

            foreach (var conversation in stale)
            {
                conversation.Status = ConversationStatus.Resolved;
                conversation.UpdatedAt = now;
            }

            await db.SaveChangesAsync(cancellationToken);
            totalResolved += stale.Count;
            logger.LogInformation("Auto-resolve: resolved {Count} stale conversations for tenant {TenantId}",
                stale.Count, tenant.Id);
        }

        logger.LogInformation("Auto-resolve run complete: {Total} conversations resolved across {TenantCount} tenants",
            totalResolved, tenants.Count);
    }

    public async Task ReassignOfflineAgentsAsync(CancellationToken cancellationToken = default)
    {
        var heartbeatCutoff = DateTime.UtcNow.Subtract(OfflineGrace);

        var offlineUserIds = await db.Users.AsNoTracking()
            .Where(u => !u.IsOnline && (u.LastSeenAt == null || u.LastSeenAt < heartbeatCutoff))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var reassignedAgents = 0;
        foreach (var userId in offlineUserIds)
        {
            try
            {
                await distribution.ReassignOpenFromAsync(userId, cancellationToken);
                reassignedAgents++;
            }
            catch (Exception ex)
            {
                // One agent failing must not abort the whole sweep.
                logger.LogError(ex, "Reassign-offline: failed to reassign open conversations for user {UserId}", userId);
            }
        }

        logger.LogInformation("Reassign-offline run complete: processed {Count} offline agents", reassignedAgents);
    }
}
