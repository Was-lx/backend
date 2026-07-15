using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Distribution;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Auto-distribution engine (Sprint 3). Routes new/unassigned conversations to the least-loaded eligible
/// agent per the owning WhatsApp number's <see cref="DistributionMode"/>. "Load" = the count of an agent's
/// non-Resolved conversations, so assigning to the least loaded gives fair round-robin plus backlog spread
/// automatically. Every eligibility query is tenant-scoped and excludes disabled (Identity IsDisabled) agents.
/// </summary>
internal sealed class DistributionService(ApplicationDbContext db, IInboxRealtimeNotifier notifier) : IDistributionService
{
    public async Task<int?> AutoAssignAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await db.Conversations
            .Include(c => c.WhatsAppAccount)
            .FirstOrDefaultAsync(c => c.Id == conversationId && !c.IsDeleted, cancellationToken);
        if (conversation is null || conversation.WhatsAppAccount is null)
            return null;

        var account = conversation.WhatsAppAccount;

        var (eligibleIds, reason) = account.DistributionMode switch
        {
            DistributionMode.RoundRobin =>
                (await GetRoundRobinEligibleAsync(account.Id, account.TenantId, account.DistributeToOffline, cancellationToken), "Round Robin"),
            DistributionMode.RoundRobinByWorkingHours =>
                (await GetWorkingHoursEligibleAsync(conversation.TenantId, account.Id, cancellationToken), "Round Robin (working hours)"),
            _ => (new List<int>(), string.Empty) // ByAdmin (and any future mode) => leave unassigned
        };

        if (eligibleIds.Count == 0)
            return null;

        var chosen = await PickLeastLoadedAsync(eligibleIds, cancellationToken);
        await AssignAsync(conversation, chosen, reason, cancellationToken);
        return chosen;
    }

    public async Task ReassignOpenFromAsync(int domainUserId, CancellationToken cancellationToken = default)
    {
        // Only OPEN conversations on RoundRobin numbers that opted into offline reassignment are affected.
        var conversations = await db.Conversations
            .Include(c => c.WhatsAppAccount)
            .Where(c => c.AssignedUserId == domainUserId
                && c.Status != ConversationStatus.Resolved
                && !c.IsDeleted
                && c.WhatsAppAccount.DistributionMode == DistributionMode.RoundRobin
                && c.WhatsAppAccount.ReassignOnOffline)
            .ToListAsync(cancellationToken);

        foreach (var conversation in conversations)
        {
            // Reassign only to an ONLINE teammate, excluding the offline agent. If nobody else is online,
            // leave the conversation with the current agent — never re-pick the offline agent (that would
            // loop forever) and never null it out (that would orphan it).
            var targets = await GetOnlineEligibleAsync(conversation.WhatsAppAccountId, conversation.TenantId, domainUserId, cancellationToken);
            if (targets.Count == 0)
                continue;

            var chosen = await PickLeastLoadedAsync(targets, cancellationToken);
            if (chosen == domainUserId)
                continue;

            await AssignAsync(conversation, chosen, "Reassigned (agent offline)", cancellationToken);
        }
    }

    /// <summary>Applies an assignment to a tracked conversation: sets owner, advances a fresh status, records the Assignment, notifies, saves.</summary>
    private async Task AssignAsync(Conversation conversation, int userId, string reason, CancellationToken cancellationToken)
    {
        conversation.AssignedUserId = userId;
        // Advance to Assigned only from a fresh state; never downgrade an in-flight conversation.
        if (conversation.Status is ConversationStatus.New or ConversationStatus.Reopened)
            conversation.Status = ConversationStatus.Assigned;
        conversation.UpdatedAt = DateTime.UtcNow;

        db.Assignments.Add(new Assignment
        {
            ConversationId = conversation.Id,
            AssignedToUserId = userId,
            Method = AssignmentMethod.RoundRobin,
            Reason = reason,
            AssignedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        await notifier.ConversationChangedAsync(conversation.TenantId, new ConversationChangedPayload(
            conversation.Id, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt), cancellationToken);
    }

    /// <summary>
    /// RoundRobin eligibility: agents in this number's distribution list, in the right tenant, not disabled,
    /// not on break, and — unless the number distributes to offline agents — currently online.
    /// </summary>
    private async Task<List<int>> GetRoundRobinEligibleAsync(int whatsAppAccountId, int tenantId, bool distributeToOffline, CancellationToken cancellationToken)
    {
        var query = db.AgentWhatsAppDistributions.AsNoTracking()
            .Where(d => d.WhatsAppAccountId == whatsAppAccountId
                && d.User.TenantId == tenantId
                && !d.User.IsOnBreak
                && !db.Set<ApplicationUser>().Any(au => au.Email == d.User.Email && au.IsDisabled));

        if (!distributeToOffline)
            query = query.Where(d => d.User.IsOnline);

        return await query.Select(d => d.UserId).Distinct().ToListAsync(cancellationToken);
    }

    /// <summary>Online, not-on-break, not-disabled agents in this number's distribution list, excluding one user (used for offline reassignment).</summary>
    private async Task<List<int>> GetOnlineEligibleAsync(int whatsAppAccountId, int tenantId, int excludeUserId, CancellationToken cancellationToken)
    {
        return await db.AgentWhatsAppDistributions.AsNoTracking()
            .Where(d => d.WhatsAppAccountId == whatsAppAccountId
                && d.UserId != excludeUserId
                && d.User.TenantId == tenantId
                && d.User.IsOnline
                && !d.User.IsOnBreak
                && !db.Set<ApplicationUser>().Any(au => au.Email == d.User.Email && au.IsDisabled))
            .Select(d => d.UserId).Distinct().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// RoundRobinByWorkingHours eligibility: distribution-list agents whose shift is active in the tenant's
    /// local "now" (today is a company working day, and a ShiftDay window contains the local time, handling
    /// windows that cross midnight). IsOnline / IsOnBreak are ignored for this mode; disabled agents are excluded.
    /// </summary>
    private async Task<List<int>> GetWorkingHoursEligibleAsync(int tenantId, int whatsAppAccountId, CancellationToken cancellationToken)
    {
        var candidateIds = await db.AgentWhatsAppDistributions.AsNoTracking()
            .Where(d => d.WhatsAppAccountId == whatsAppAccountId
                && d.User.TenantId == tenantId
                && !db.Set<ApplicationUser>().Any(au => au.Email == d.User.Email && au.IsDisabled))
            .Select(d => d.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return new List<int>();

        var timeZoneId = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ResolveTimeZone(timeZoneId));
        var today = localNow.DayOfWeek;
        var nowTime = TimeOnly.FromDateTime(localNow);

        var companyOpen = await db.CompanyWorkingDays.AsNoTracking()
            .AnyAsync(d => d.TenantId == tenantId && d.DayOfWeek == today && d.IsWorkingDay, cancellationToken);
        if (!companyOpen)
            return new List<int>();

        return await db.AgentShifts.AsNoTracking()
            .Where(a => candidateIds.Contains(a.UserId)
                && a.Shift.TenantId == tenantId
                && a.Shift.ShiftDays.Any(sd => sd.DayOfWeek == today
                    && (sd.StartTime <= sd.EndTime
                        ? (nowTime >= sd.StartTime && nowTime <= sd.EndTime)
                        : (nowTime >= sd.StartTime || nowTime <= sd.EndTime))))
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Picks the least-loaded eligible agent. Load = count of non-Resolved conversations owned by the agent.
    /// Ties break on the oldest most-recent assignment (never-assigned agents rank first), then lowest UserId.
    /// </summary>
    private async Task<int> PickLeastLoadedAsync(List<int> eligibleIds, CancellationToken cancellationToken)
    {
        var loads = await db.Conversations.AsNoTracking()
            .Where(c => c.AssignedUserId != null
                && eligibleIds.Contains(c.AssignedUserId.Value)
                && c.Status != ConversationStatus.Resolved
                && !c.IsDeleted)
            .GroupBy(c => c.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lastAssigned = await db.Assignments.AsNoTracking()
            .Where(a => eligibleIds.Contains(a.AssignedToUserId))
            .GroupBy(a => a.AssignedToUserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(a => a.AssignedAt) })
            .ToListAsync(cancellationToken);

        var loadMap = loads.ToDictionary(x => x.UserId, x => x.Count);
        var lastMap = lastAssigned.ToDictionary(x => x.UserId, x => x.Last);

        return eligibleIds
            .OrderBy(id => loadMap.TryGetValue(id, out var count) ? count : 0)
            .ThenBy(id => lastMap.TryGetValue(id, out var last) ? last : DateTime.MinValue)
            .ThenBy(id => id)
            .First();
    }

    /// <summary>Resolves the tenant's time zone id, falling back to UTC on a null or invalid id.</summary>
    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
