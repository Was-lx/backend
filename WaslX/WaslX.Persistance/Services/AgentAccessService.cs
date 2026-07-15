using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.AgentAccess;
using WaslX.Application.Features.AgentAccess.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class AgentAccessService(ApplicationDbContext db) : IAgentAccessService
{
    public async Task<Result<AgentAccessResponse>> GetAccessAsync(int? tenantId, string appUserId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<AgentAccessResponse>(AppErrors.NoTenantContext);

        var identity = await ResolveIdentityAsync(tid, appUserId, cancellationToken);
        if (identity is null)
            return Result.Failure<AgentAccessResponse>(AppErrors.UserContextNotResolved);

        // The domain user may not have been mirrored yet (no join rows) — return empty sets in that case.
        var userId = string.IsNullOrEmpty(identity.Email)
            ? (int?)null
            : await db.Users.Where(u => u.TenantId == tid && u.Email == identity.Email)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

        if (userId is not { } uid)
            return Result.Success(new AgentAccessResponse(appUserId, [], [], [], []));

        return Result.Success(await BuildResponseAsync(appUserId, uid, cancellationToken));
    }

    public async Task<Result<AgentAccessResponse>> SetAccessAsync(int? tenantId, string appUserId, SetAgentAccessRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<AgentAccessResponse>(AppErrors.NoTenantContext);

        var identity = await ResolveIdentityAsync(tid, appUserId, cancellationToken);
        if (identity is null)
            return Result.Failure<AgentAccessResponse>(AppErrors.UserContextNotResolved);

        var uid = await ResolveOrCreateDomainUserIdAsync(tid, identity, cancellationToken);

        // 1) Channels the agent can access — keep only channels owned by the tenant.
        var desiredChannels = await FilterOwnedAsync(request.ChannelIds, ids =>
            db.Channels.Where(c => c.TenantId == tid && ids.Contains(c.Id)).Select(c => c.Id), cancellationToken);
        await ReconcileChannelAccessAsync(uid, desiredChannels, cancellationToken);

        // 2) Distribution numbers — must be owned by the tenant AND sit inside one of the agent's channels.
        var desiredDistribution = await FilterOwnedAsync(request.DistributionWhatsAppAccountIds, ids =>
            db.ChannelWhatsAppAccounts
                .Where(l => ids.Contains(l.WhatsAppAccountId)
                            && l.WhatsAppAccount.TenantId == tid
                            && desiredChannels.Contains(l.ChannelId))
                .Select(l => l.WhatsAppAccountId), cancellationToken);
        await ReconcileDistributionAsync(uid, desiredDistribution, cancellationToken);

        // 3) Groups the agent belongs to — keep only groups owned by the tenant.
        var desiredGroups = await FilterOwnedAsync(request.GroupIds, ids =>
            db.Groups.Where(g => g.TenantId == tid && ids.Contains(g.Id)).Select(g => g.Id), cancellationToken);
        await ReconcileGroupsAsync(uid, desiredGroups, cancellationToken);

        // 4) Shifts the agent works — keep only shifts owned by the tenant.
        var desiredShifts = await FilterOwnedAsync(request.ShiftIds, ids =>
            db.Shifts.Where(s => s.TenantId == tid && ids.Contains(s.Id)).Select(s => s.Id), cancellationToken);
        await ReconcileShiftsAsync(uid, desiredShifts, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await BuildResponseAsync(appUserId, uid, cancellationToken));
    }

    private async Task<AgentAccessResponse> BuildResponseAsync(string appUserId, int userId, CancellationToken cancellationToken)
    {
        var channelIds = await db.AgentChannelAccesses.AsNoTracking()
            .Where(a => a.UserId == userId).Select(a => a.ChannelId).OrderBy(id => id).ToListAsync(cancellationToken);
        var distributionIds = await db.AgentWhatsAppDistributions.AsNoTracking()
            .Where(a => a.UserId == userId).Select(a => a.WhatsAppAccountId).OrderBy(id => id).ToListAsync(cancellationToken);
        var groupIds = await db.UserGroups.AsNoTracking()
            .Where(g => g.UserId == userId).Select(g => g.GroupId).OrderBy(id => id).ToListAsync(cancellationToken);
        var shiftIds = await db.AgentShifts.AsNoTracking()
            .Where(s => s.UserId == userId).Select(s => s.ShiftId).OrderBy(id => id).ToListAsync(cancellationToken);

        return new AgentAccessResponse(appUserId, channelIds, distributionIds, groupIds, shiftIds);
    }

    /// <summary>Resolves the ApplicationUser (Identity) row for the given id, scoped to this tenant.</summary>
    private Task<ApplicationUser?> ResolveIdentityAsync(int tenantId, string appUserId, CancellationToken cancellationToken) =>
        db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == appUserId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Intersects the requested ids with the ids the given query proves are owned by the tenant.</summary>
    private static async Task<HashSet<int>> FilterOwnedAsync(
        IReadOnlyList<int>? requestedIds,
        Func<HashSet<int>, IQueryable<int>> ownedQuery,
        CancellationToken cancellationToken)
    {
        if (requestedIds is null || requestedIds.Count == 0)
            return [];
        var desired = requestedIds.ToHashSet();
        var owned = await ownedQuery(desired).ToListAsync(cancellationToken);
        return owned.ToHashSet();
    }

    private async Task ReconcileChannelAccessAsync(int userId, HashSet<int> desired, CancellationToken cancellationToken)
    {
        var existing = await db.AgentChannelAccesses.Where(a => a.UserId == userId).ToListAsync(cancellationToken);
        var existingIds = existing.Select(a => a.ChannelId).ToHashSet();

        var toRemove = existing.Where(a => !desired.Contains(a.ChannelId)).ToList();
        if (toRemove.Count > 0) db.AgentChannelAccesses.RemoveRange(toRemove);

        foreach (var id in desired.Where(d => !existingIds.Contains(d)))
            db.AgentChannelAccesses.Add(new AgentChannelAccess { UserId = userId, ChannelId = id });
    }

    private async Task ReconcileDistributionAsync(int userId, HashSet<int> desired, CancellationToken cancellationToken)
    {
        var existing = await db.AgentWhatsAppDistributions.Where(a => a.UserId == userId).ToListAsync(cancellationToken);
        var existingIds = existing.Select(a => a.WhatsAppAccountId).ToHashSet();

        var toRemove = existing.Where(a => !desired.Contains(a.WhatsAppAccountId)).ToList();
        if (toRemove.Count > 0) db.AgentWhatsAppDistributions.RemoveRange(toRemove);

        foreach (var id in desired.Where(d => !existingIds.Contains(d)))
            db.AgentWhatsAppDistributions.Add(new AgentWhatsAppDistribution { UserId = userId, WhatsAppAccountId = id });
    }

    private async Task ReconcileGroupsAsync(int userId, HashSet<int> desired, CancellationToken cancellationToken)
    {
        var existing = await db.UserGroups.Where(g => g.UserId == userId).ToListAsync(cancellationToken);
        var existingIds = existing.Select(g => g.GroupId).ToHashSet();

        var toRemove = existing.Where(g => !desired.Contains(g.GroupId)).ToList();
        if (toRemove.Count > 0) db.UserGroups.RemoveRange(toRemove);

        foreach (var id in desired.Where(d => !existingIds.Contains(d)))
            db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = id });
    }

    private async Task ReconcileShiftsAsync(int userId, HashSet<int> desired, CancellationToken cancellationToken)
    {
        var existing = await db.AgentShifts.Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        var existingIds = existing.Select(s => s.ShiftId).ToHashSet();

        var toRemove = existing.Where(s => !desired.Contains(s.ShiftId)).ToList();
        if (toRemove.Count > 0) db.AgentShifts.RemoveRange(toRemove);

        foreach (var id in desired.Where(d => !existingIds.Contains(d)))
            db.AgentShifts.Add(new AgentShift { UserId = userId, ShiftId = id });
    }

    /// <summary>
    /// Returns the domain <see cref="User"/> id that mirrors the Identity user, creating it (and its domain
    /// role, since users.RoleId is required) the first time it's needed. Mirrors NoteService's find-or-create.
    /// </summary>
    private async Task<int> ResolveOrCreateDomainUserIdAsync(int tenantId, ApplicationUser identity, CancellationToken cancellationToken)
    {
        var email = identity.Email ?? string.Empty;
        if (!string.IsNullOrEmpty(email))
        {
            var existingId = await db.Users
                .Where(u => u.TenantId == tenantId && u.Email == email)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingId is { } id)
                return id;
        }

        var roleId = await db.Set<Role>()
            .Where(r => r.Name == "Agent")
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (roleId is null)
        {
            var role = new Role { Name = "Agent", Description = "Agent" };
            await db.Set<Role>().AddAsync(role, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            roleId = role.Id;
        }

        var displayName = string.IsNullOrWhiteSpace(identity.FullName) ? (email.Length > 0 ? email : "User") : identity.FullName;
        var user = new User
        {
            TenantId = tenantId,
            RoleId = roleId.Value,
            Name = displayName,
            Email = email,
            PasswordHash = string.Empty,
            Status = "Active"
        };
        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
