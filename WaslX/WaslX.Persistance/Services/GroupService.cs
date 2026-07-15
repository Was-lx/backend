using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Groups;
using WaslX.Application.Features.Groups.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class GroupService(ApplicationDbContext db) : IGroupService
{
    public async Task<Result<IReadOnlyList<GroupResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<GroupResponse>>(AppErrors.NoTenantContext);

        var groups = await db.Groups.AsNoTracking()
            .Where(g => g.TenantId == tid)
            .OrderByDescending(g => g.IsDefault)
            .ThenBy(g => g.Name)
            .Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Description,
                g.IsDefault,
                g.IsAssignableByDistribution,
                g.Stages.OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id)
                    .Select(s => new StageResponse(s.Id, s.Name, s.SequenceOrder)).ToList(),
                g.UserGroups.Count))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupResponse>>(groups);
    }

    public async Task<Result<GroupResponse>> GetByIdAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        var group = await db.Groups.AsNoTracking()
            .Where(g => g.TenantId == tid && g.Id == id)
            .Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Description,
                g.IsDefault,
                g.IsAssignableByDistribution,
                g.Stages.OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id)
                    .Select(s => new StageResponse(s.Id, s.Name, s.SequenceOrder)).ToList(),
                g.UserGroups.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return group is null
            ? Result.Failure<GroupResponse>(AppErrors.GroupNotFound)
            : Result.Success(group);
    }

    public async Task<Result<GroupResponse>> CreateAsync(int? tenantId, UpsertGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<GroupResponse>(AppErrors.GroupNameRequired);
        if (await db.Groups.AnyAsync(g => g.TenantId == tid && g.Name == name, cancellationToken))
            return Result.Failure<GroupResponse>(AppErrors.DuplicateGroupName);

        var group = new Group
        {
            TenantId = tid,
            Name = name,
            Description = request.Description?.Trim() ?? string.Empty,
            IsAssignableByDistribution = request.IsAssignableByDistribution
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, group.Id, cancellationToken);
    }

    public async Task<Result<GroupResponse>> UpdateAsync(int? tenantId, int id, UpsertGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        var group = await db.Groups.FirstOrDefaultAsync(g => g.TenantId == tid && g.Id == id, cancellationToken);
        if (group is null)
            return Result.Failure<GroupResponse>(AppErrors.GroupNotFound);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<GroupResponse>(AppErrors.GroupNameRequired);
        if (name != group.Name && await db.Groups.AnyAsync(g => g.TenantId == tid && g.Name == name && g.Id != id, cancellationToken))
            return Result.Failure<GroupResponse>(AppErrors.DuplicateGroupName);

        group.Name = name;
        group.Description = request.Description?.Trim() ?? string.Empty;
        group.IsAssignableByDistribution = request.IsAssignableByDistribution;
        group.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, group.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var group = await db.Groups.FirstOrDefaultAsync(g => g.TenantId == tid && g.Id == id, cancellationToken);
        if (group is null)
            return Result.Failure(AppErrors.GroupNotFound);

        var stageIds = await db.Stages.Where(s => s.GroupId == id).Select(s => s.Id).ToListAsync(cancellationToken);

        // Block deletion while anything still points at the group or its stages (all FKs are Restrict).
        var referenced =
            await db.Conversations.AnyAsync(c => c.GroupId == id, cancellationToken) ||
            (stageIds.Count > 0 && (
                await db.Conversations.AnyAsync(c => c.CurrentStageId != null && stageIds.Contains(c.CurrentStageId.Value), cancellationToken) ||
                await db.ConversationStageHistories.AnyAsync(h => stageIds.Contains(h.StageId), cancellationToken)));
        if (referenced)
            return Result.Failure(AppErrors.GroupInUse);

        // Safe to clear dependents (memberships + stages), then the group itself.
        var memberships = await db.UserGroups.Where(m => m.GroupId == id).ToListAsync(cancellationToken);
        var stages = await db.Stages.Where(s => s.GroupId == id).ToListAsync(cancellationToken);
        db.UserGroups.RemoveRange(memberships);
        db.Stages.RemoveRange(stages);
        db.Groups.Remove(group);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // ── Stages ──

    public async Task<Result<GroupResponse>> CreateStageAsync(int? tenantId, int groupId, UpsertStageRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        if (!await db.Groups.AnyAsync(g => g.TenantId == tid && g.Id == groupId, cancellationToken))
            return Result.Failure<GroupResponse>(AppErrors.GroupNotFound);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<GroupResponse>(AppErrors.StageNameRequired);

        var maxOrder = await db.Stages.Where(s => s.GroupId == groupId)
            .Select(s => (int?)s.SequenceOrder).MaxAsync(cancellationToken) ?? 0;

        db.Stages.Add(new Stage { GroupId = groupId, Name = name, SequenceOrder = maxOrder + 1 });
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, groupId, cancellationToken);
    }

    public async Task<Result<GroupResponse>> UpdateStageAsync(int? tenantId, int groupId, int stageId, UpsertStageRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        var stage = await db.Stages
            .FirstOrDefaultAsync(s => s.Id == stageId && s.GroupId == groupId && s.Group.TenantId == tid, cancellationToken);
        if (stage is null)
            return Result.Failure<GroupResponse>(AppErrors.StageNotFound);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<GroupResponse>(AppErrors.StageNameRequired);

        stage.Name = name;
        stage.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, groupId, cancellationToken);
    }

    public async Task<Result<GroupResponse>> DeleteStageAsync(int? tenantId, int groupId, int stageId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        var stage = await db.Stages
            .FirstOrDefaultAsync(s => s.Id == stageId && s.GroupId == groupId && s.Group.TenantId == tid, cancellationToken);
        if (stage is null)
            return Result.Failure<GroupResponse>(AppErrors.StageNotFound);

        // Block while conversations or history still reference the stage (FKs are Restrict).
        var referenced =
            await db.Conversations.AnyAsync(c => c.CurrentStageId == stageId, cancellationToken) ||
            await db.ConversationStageHistories.AnyAsync(h => h.StageId == stageId, cancellationToken);
        if (referenced)
            return Result.Failure<GroupResponse>(AppErrors.GroupInUse);

        db.Stages.Remove(stage);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, groupId, cancellationToken);
    }

    public async Task<Result<GroupResponse>> ReorderStagesAsync(int? tenantId, int groupId, ReorderStagesRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        if (!await db.Groups.AnyAsync(g => g.TenantId == tid && g.Id == groupId, cancellationToken))
            return Result.Failure<GroupResponse>(AppErrors.GroupNotFound);

        var stages = await db.Stages.Where(s => s.GroupId == groupId).ToListAsync(cancellationToken);
        var byId = stages.ToDictionary(s => s.Id);

        var order = 1;
        // Apply the requested order first; any stages not listed keep a stable trailing order.
        foreach (var stageId in request.StageIdsInOrder ?? [])
        {
            if (byId.TryGetValue(stageId, out var stage))
            {
                stage.SequenceOrder = order++;
                stage.UpdatedAt = DateTime.UtcNow;
                byId.Remove(stageId);
            }
        }
        foreach (var stage in byId.Values.OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id))
        {
            stage.SequenceOrder = order++;
            stage.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(tid, groupId, cancellationToken);
    }

    // ── Membership ──

    public async Task<Result<GroupResponse>> AddMemberAsync(int? tenantId, int groupId, string userId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        if (!await db.Groups.AnyAsync(g => g.TenantId == tid && g.Id == groupId, cancellationToken))
            return Result.Failure<GroupResponse>(AppErrors.GroupNotFound);

        var domainUser = await ResolveOrCreateDomainUserAsync(tid, userId, cancellationToken);
        if (domainUser is null)
            return Result.Failure<GroupResponse>(AppErrors.UserContextNotResolved);

        var exists = await db.UserGroups.AnyAsync(m => m.GroupId == groupId && m.UserId == domainUser.Id, cancellationToken);
        if (!exists)
        {
            db.UserGroups.Add(new UserGroup { GroupId = groupId, UserId = domainUser.Id });
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(tid, groupId, cancellationToken);
    }

    public async Task<Result<GroupResponse>> RemoveMemberAsync(int? tenantId, int groupId, string userId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        if (!await db.Groups.AnyAsync(g => g.TenantId == tid && g.Id == groupId, cancellationToken))
            return Result.Failure<GroupResponse>(AppErrors.GroupNotFound);

        // Resolve the domain user without creating one; nothing to remove if it doesn't exist.
        var appUser = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == userId && u.TenantId == tid)
            .Select(u => new { u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (appUser?.Email is { } email)
        {
            var membership = await db.UserGroups
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.User.TenantId == tid && m.User.Email == email, cancellationToken);
            if (membership is not null)
            {
                db.UserGroups.Remove(membership);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return await GetByIdAsync(tid, groupId, cancellationToken);
    }

    // ── Default group bootstrap ──

    public async Task<Result<GroupResponse>> EnsureDefaultSalesGroupAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<GroupResponse>(AppErrors.NoTenantContext);

        var existing = await db.Groups
            .Where(g => g.TenantId == tid && g.IsDefault)
            .Select(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != 0)
            return await GetByIdAsync(tid, existing, cancellationToken);

        var group = new Group
        {
            TenantId = tid,
            Name = "Sales",
            Description = string.Empty,
            IsDefault = true,
            IsAssignableByDistribution = true
        };
        group.Stages.Add(new Stage { Name = "New", SequenceOrder = 1 });
        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tid, group.Id, cancellationToken);
    }

    /// <summary>
    /// Resolves the domain <see cref="User"/> that mirrors an ApplicationUser (Identity) id within the
    /// tenant, creating the domain row on first use (find-or-create by tenant + email). Returns null when
    /// the ApplicationUser doesn't exist / isn't in this tenant.
    /// </summary>
    private async Task<User?> ResolveOrCreateDomainUserAsync(int tenantId, string applicationUserId, CancellationToken cancellationToken)
    {
        var appUser = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == applicationUserId && u.TenantId == tenantId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        if (appUser is null || string.IsNullOrEmpty(appUser.Email))
            return null;

        var existing = await db.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == appUser.Email, cancellationToken);
        if (existing is not null)
            return existing;

        // users.RoleId is a required FK; default new mirror rows to the Agent domain role (create if missing).
        var roleId = await db.Roles.Where(r => r.Name == "Agent").Select(r => (int?)r.Id).FirstOrDefaultAsync(cancellationToken);
        if (roleId is null)
        {
            var role = new Role { Name = "Agent", Description = "Agent" };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
            roleId = role.Id;
        }

        var user = new User
        {
            TenantId = tenantId,
            RoleId = roleId.Value,
            Name = string.IsNullOrWhiteSpace(appUser.FullName) ? appUser.Email : appUser.FullName,
            Email = appUser.Email,
            PasswordHash = string.Empty,
            Status = "Active"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
