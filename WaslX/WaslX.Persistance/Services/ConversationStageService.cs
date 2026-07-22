using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.ConversationStages;
using WaslX.Application.Abstractions.Performance;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.ConversationStages.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Stage pipeline + cross-team handoff. Every write is scoped to the caller's tenant AND their ownership:
/// a non-privileged agent can only route/move/hand off a conversation assigned to them (managers/admins may
/// act on any), mirroring <see cref="ConversationService"/>'s access check. The "changed by" user recorded
/// in history is the domain user id (from the JWT <c>duid</c> claim) with an email fallback.
/// </summary>
internal sealed class ConversationStageService(ApplicationDbContext db, IInboxRealtimeNotifier notifier, IAgentPerformanceUpdateService performanceUpdate) : IConversationStageService
{
    public async Task<Result> RouteToGroupAsync(
        int? tenantId, int conversationId, int groupId, int byUserId, string? byUserEmail, bool isPrivileged, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted, cancellationToken);
        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        if (!await db.Groups.AnyAsync(g => g.Id == groupId && g.TenantId == tid, cancellationToken))
            return Result.Failure(AppErrors.GroupNotFound);

        var changedBy = await ResolveChangedByUserIdAsync(tid, byUserId, byUserEmail, cancellationToken);
        if (changedBy is null)
            return Result.Failure(AppErrors.UserContextNotResolved);

        if (!isPrivileged && conversation.AssignedUserId != changedBy)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        var firstStageId = await FirstStageIdAsync(groupId, cancellationToken);

        conversation.GroupId = groupId;
        conversation.CurrentStageId = firstStageId;
        conversation.UpdatedAt = DateTime.UtcNow;

        WriteStageHistory(conversation.Id, firstStageId, changedBy.Value);
        await db.SaveChangesAsync(cancellationToken);

        await NotifyAsync(tid, conversation, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MoveToStageAsync(
        int? tenantId, int conversationId, int stageId, int byUserId, string? byUserEmail, bool isPrivileged, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted, cancellationToken);
        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        // The stage must belong to the conversation's CURRENT group (and therefore this tenant). A
        // conversation with no group yet can't have a stage moved on it.
        if (conversation.GroupId is not { } groupId)
            return Result.Failure(AppErrors.StageNotInGroup);

        var stageOk = await db.Stages.AnyAsync(
            s => s.Id == stageId && s.GroupId == groupId && s.Group.TenantId == tid, cancellationToken);
        if (!stageOk)
            return Result.Failure(AppErrors.StageNotInGroup);

        var changedBy = await ResolveChangedByUserIdAsync(tid, byUserId, byUserEmail, cancellationToken);
        if (changedBy is null)
            return Result.Failure(AppErrors.UserContextNotResolved);

        if (!isPrivileged && conversation.AssignedUserId != changedBy)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        conversation.CurrentStageId = stageId;
        conversation.UpdatedAt = DateTime.UtcNow;

        WriteStageHistory(conversation.Id, stageId, changedBy.Value);
        await db.SaveChangesAsync(cancellationToken);

        await NotifyAsync(tid, conversation, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> HandoffAsync(
        int? tenantId, int conversationId, int targetGroupId, int byUserId, string? byUserEmail, bool isPrivileged, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted, cancellationToken);
        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        if (!await db.Groups.AnyAsync(g => g.Id == targetGroupId && g.TenantId == tid, cancellationToken))
            return Result.Failure(AppErrors.GroupNotFound);

        var changedBy = await ResolveChangedByUserIdAsync(tid, byUserId, byUserEmail, cancellationToken);
        if (changedBy is null)
            return Result.Failure(AppErrors.UserContextNotResolved);

        // An agent may only hand off a conversation they own (US-3.6); managers/admins may hand off any.
        if (!isPrivileged && conversation.AssignedUserId != changedBy)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        var firstStageId = await FirstStageIdAsync(targetGroupId, cancellationToken);

        // Cross-team handoff: move to the target team's first stage and drop the current owner so the
        // receiving team picks it up via their own queue / distribution. The full message + note history
        // stays intact because it's the same conversation row.
        var previousOwnerId = conversation.AssignedUserId;

        conversation.GroupId = targetGroupId;
        conversation.CurrentStageId = firstStageId;
        conversation.AssignedUserId = null;
        // A resolved conversation being handed off should re-enter the flow; anything else keeps its status.
        if (conversation.Status == ConversationStatus.Resolved)
            conversation.Status = ConversationStatus.New;
        conversation.UpdatedAt = DateTime.UtcNow;

        WriteStageHistory(conversation.Id, firstStageId, changedBy.Value);
        await db.SaveChangesAsync(cancellationToken);

        if (previousOwnerId is { } prevId)
            await performanceUpdate.RecordConversationClosedAsync(prevId, resolved: false, cancellationToken);

        await NotifyAsync(tid, conversation, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<StageBoardColumn>>> GetBoardAsync(
        int? tenantId, int groupId, bool isPrivileged, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<StageBoardColumn>>(AppErrors.NoTenantContext);

        if (!await db.Groups.AnyAsync(g => g.Id == groupId && g.TenantId == tid, cancellationToken))
            return Result.Failure<IReadOnlyList<StageBoardColumn>>(AppErrors.GroupNotFound);

        var stages = await db.Stages.AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.Name, s.SequenceOrder })
            .ToListAsync(cancellationToken);

        var convQuery = db.Conversations.AsNoTracking()
            .Where(c => c.TenantId == tid && c.GroupId == groupId && !c.IsDeleted);

        // Agents only see their own conversations on the board; managers/admins see the whole team.
        if (!isPrivileged)
            convQuery = convQuery.Where(c => c.AssignedUserId == currentUserId);

        var rows = await convQuery
            .OrderByDescending(c => c.LastMessageAt).ThenByDescending(c => c.Id)
            .Select(c => new
            {
                c.CurrentStageId,
                Item = new BoardConversationItem(
                    c.Id,
                    c.Customer.Name,
                    c.Customer.PhoneNumber,
                    c.Status.ToString(),
                    c.AssignedUser != null ? c.AssignedUser.Name : null,
                    c.LastMessageAt)
            })
            .ToListAsync(cancellationToken);

        var columns = stages
            .Select(s => new StageBoardColumn(
                s.Id, s.Name, s.SequenceOrder,
                rows.Where(r => r.CurrentStageId == s.Id).Select(r => r.Item).ToList()))
            .ToList();

        // Surface conversations routed to the group but not yet in any stage as a leading "Unstaged" column.
        var unstaged = rows.Where(r => r.CurrentStageId is null).Select(r => r.Item).ToList();
        if (unstaged.Count > 0)
            columns.Insert(0, new StageBoardColumn(null, "Unstaged", 0, unstaged));

        return Result.Success<IReadOnlyList<StageBoardColumn>>(columns);
    }

    /// <summary>First stage of a group (lowest SequenceOrder); null when the group has no stages.</summary>
    private Task<int?> FirstStageIdAsync(int groupId, CancellationToken cancellationToken) =>
        db.Stages.Where(s => s.GroupId == groupId)
            .OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Adds a stage-history row (no-op when there is no stage, since StageId is a required FK).</summary>
    private void WriteStageHistory(int conversationId, int? stageId, int changedByUserId)
    {
        if (stageId is not { } sid)
            return;

        db.ConversationStageHistories.Add(new ConversationStageHistory
        {
            ConversationId = conversationId,
            StageId = sid,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow
        });
    }

    private Task NotifyAsync(int tenantId, Conversation conversation, CancellationToken cancellationToken) =>
        notifier.ConversationChangedAsync(tenantId, new ConversationChangedPayload(
            conversation.Id, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt), cancellationToken);

    /// <summary>
    /// Resolves the domain user id to record as the "changed by" actor. Prefers the JWT domain user id
    /// (duid) when it maps to a user in this tenant, falling back to a tenant + email lookup. Returns null
    /// when neither resolves (so the caller can fail cleanly instead of violating the history FK).
    /// </summary>
    private async Task<int?> ResolveChangedByUserIdAsync(int tenantId, int byUserId, string? byUserEmail, CancellationToken cancellationToken)
    {
        if (byUserId > 0 && await db.Users.AnyAsync(u => u.Id == byUserId && u.TenantId == tenantId, cancellationToken))
            return byUserId;

        if (!string.IsNullOrEmpty(byUserEmail))
            return await db.Users.Where(u => u.TenantId == tenantId && u.Email == byUserEmail)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

        return null;
    }
}
