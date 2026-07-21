using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Assignments;
using WaslX.Application.Abstractions.Audit;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Assignments.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class AssignmentService(ApplicationDbContext db, IInboxRealtimeNotifier notifier, INotificationService notifications, IAuditService audit) : IAssignmentService
{
    public Task<Result> AssignAsync(int? tenantId, int conversationId, string targetAppUserId, string byAppUserId, string? reason, CancellationToken cancellationToken = default) =>
        AssignInternalAsync(tenantId, conversationId, targetAppUserId, byAppUserId, reason, isReassign: false, cancellationToken);

    // Reassignment is the same operation (Method=Manual); it simply changes the owner to a new agent.
    public Task<Result> ReassignAsync(int? tenantId, int conversationId, string targetAppUserId, string byAppUserId, string? reason, CancellationToken cancellationToken = default) =>
        AssignInternalAsync(tenantId, conversationId, targetAppUserId, byAppUserId, reason, isReassign: true, cancellationToken);

    private async Task<Result> AssignInternalAsync(int? tenantId, int conversationId, string targetAppUserId, string byAppUserId, string? reason, bool isReassign, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted, cancellationToken);
        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        var target = await ResolveOrCreateDomainUserAsync(tid, targetAppUserId, cancellationToken);
        if (target is null)
            return Result.Failure(AppErrors.UserContextNotResolved);

        conversation.AssignedUserId = target.Value.Id;
        // Advance to Assigned only when the lifecycle state machine permits it (e.g. New/Reopened/InProgress/Pending);
        // never force a Resolved conversation into Assigned via an assignment.
        if (conversation.Status != ConversationStatus.Assigned &&
            ConversationStatusTransitions.CanTransition(conversation.Status, ConversationStatus.Assigned))
            conversation.Status = ConversationStatus.Assigned;
        conversation.UpdatedAt = DateTime.UtcNow;

        db.Assignments.Add(new Assignment
        {
            ConversationId = conversation.Id,
            AssignedToUserId = target.Value.Id,
            Method = AssignmentMethod.Manual,
            Reason = reason ?? string.Empty,
            AssignedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        await notifier.ConversationChangedAsync(tid, new ConversationChangedPayload(
            conversation.Id, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt), cancellationToken);

        // Immutable audit trail (FR-AUDIT). Best-effort: the assignment is already persisted, so a
        // failure here (incl. resolving the actor) must never surface to the caller.
        try
        {
            var actorId = await ResolveActorDomainUserIdAsync(tid, byAppUserId, cancellationToken);
            await audit.WriteAsync(
                tid, actorId, AuditAction.Updated, "Conversation", conversation.Id.ToString(),
                isReassign ? $"Conversation reassigned to {target.Value.Name}" : $"Conversation assigned to {target.Value.Name}",
                cancellationToken);
        }
        catch
        {
            // Swallow: assignment already succeeded and was persisted.
        }

        // Notify the newly-assigned agent. Best-effort: never let a notification failure break assignment.
        try
        {
            await notifications.CreateAsync(
                tid, target.Value.Id, "assignment",
                "New conversation assigned",
                "A conversation was assigned to you.",
                "conversation", conversation.Id, cancellationToken);
        }
        catch
        {
            // Swallow: assignment already succeeded and was persisted.
        }

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AssignmentHistoryItem>>> GetHistoryAsync(int? tenantId, int conversationId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<AssignmentHistoryItem>>(AppErrors.NoTenantContext);

        // Assignment has no TenantId of its own; scope through the owning (tenant-owned) conversation.
        var exists = await db.Conversations.AsNoTracking()
            .AnyAsync(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted, cancellationToken);
        if (!exists)
            return Result.Failure<IReadOnlyList<AssignmentHistoryItem>>(AppErrors.ConversationNotFound);

        var items = await db.Assignments.AsNoTracking()
            .Where(a => a.ConversationId == conversationId)
            .OrderByDescending(a => a.AssignedAt)
            .ThenByDescending(a => a.Id)
            .Select(a => new AssignmentHistoryItem(
                a.Id,
                a.AssignedToUser.Name,
                null, // Assignment has no "assigned-by" column; kept nullable for a future schema addition.
                a.Method.ToString(),
                a.Reason == "" ? null : a.Reason,
                a.AssignedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AssignmentHistoryItem>>(items);
    }

    public async Task<Result<IReadOnlyList<UnassignedConversationItem>>> GetUnassignedAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<UnassignedConversationItem>>(AppErrors.NoTenantContext);

        var items = await db.Conversations.AsNoTracking()
            .Where(c => c.TenantId == tid && !c.IsDeleted && c.AssignedUserId == null)
            .OrderByDescending(c => c.LastMessageAt)
            .ThenByDescending(c => c.Id)
            .Select(c => new UnassignedConversationItem(
                c.Id,
                c.Customer.Name,
                c.Customer.PhoneNumber,
                c.LastMessageAt,
                c.WhatsAppAccountId))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<UnassignedConversationItem>>(items);
    }

    /// <summary>
    /// Best-effort lookup of the acting user's domain <see cref="User"/> id from their Identity id
    /// (tenant + email match). Read-only — unlike the target resolver it never creates a row, so it is
    /// safe to call purely for audit attribution. Returns null when the actor cannot be resolved.
    /// </summary>
    private async Task<int?> ResolveActorDomainUserIdAsync(int tenantId, string appUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appUserId))
            return null;

        var email = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == appUserId && u.TenantId == tenantId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(email))
            return null;

        return await db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves the domain <see cref="User"/> that mirrors an Identity (ApplicationUser) id, matching by
    /// tenant + email, and mirroring the Identity user into a domain user row on first use (users.RoleId is
    /// a required FK, so a domain role is found-or-created too). Mirrors the NoteService find-or-create pattern.
    /// </summary>
    private async Task<(int Id, string Name)?> ResolveOrCreateDomainUserAsync(int tenantId, string appUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appUserId))
            return null;

        var appUser = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == appUserId && u.TenantId == tenantId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        if (appUser is null || string.IsNullOrEmpty(appUser.Email))
            return null;

        var existing = await db.Users
            .Where(u => u.TenantId == tenantId && u.Email == appUser.Email)
            .Select(u => new { u.Id, u.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            return (existing.Id, existing.Name);

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

        var displayName = string.IsNullOrWhiteSpace(appUser.FullName) ? appUser.Email : appUser.FullName;
        var user = new User
        {
            TenantId = tenantId,
            RoleId = roleId.Value,
            Name = displayName,
            Email = appUser.Email,
            PasswordHash = string.Empty,
            Status = "Active"
        };
        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (user.Id, user.Name);
    }
}
