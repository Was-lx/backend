using Microsoft.EntityFrameworkCore;
using WaslX.Domain.Entities;
using WaslX.Infrastructure.Identity;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class ConversationService(
    ApplicationDbContext db,
    IWhatsAppService whatsApp,
    IMediaStorageService mediaStorage,
    IInboxRealtimeNotifier notifier,
    IConversationWindowService windowService) : IConversationService
{
    public async Task<Result<PagedResult<ConversationListItemResponse>>> GetConversationsAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int page, int pageSize,
        ConversationFilter? filter = null, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<PagedResult<ConversationListItemResponse>>(AppErrors.NoTenantContext);

        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Conversations.AsNoTracking().Where(c => c.TenantId == tid && !c.IsDeleted);

        // Agents see only their own assignments; managers/admins see the whole tenant.
        if (!isPrivileged)
            query = query.Where(c => c.AssignedUserId == currentUserId);

        // Resolve the optional assignee filter: the caller sends the Identity (GUID) id, but
        // AssignedUserId stores the numeric domain user id. Map GUID -> email -> domain user id.
        int? resolvedAssignedUserId = null;
        if (!string.IsNullOrWhiteSpace(filter?.AssignedUserId))
        {
            var assigneeEmail = await db.Set<ApplicationUser>().AsNoTracking()
                .Where(au => au.Id == filter.AssignedUserId)
                .Select(au => au.Email)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrEmpty(assigneeEmail))
                resolvedAssignedUserId = await db.Users.AsNoTracking()
                    .Where(u => u.TenantId == tid && u.Email == assigneeEmail)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync(cancellationToken);
        }

        // US-3.9: optional filters/search, applied AFTER tenant + role scoping so they can only
        // narrow the caller's visible set. A null (or all-null) filter leaves the default inbox intact.
        query = ApplyFilter(query, filter, resolvedAssignedUserId);

        var rows = await query
            .OrderByDescending(c => c.LastMessageAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(c => new ConversationListItemResponse(
                c.Id,
                c.Customer.Name,
                c.Customer.PhoneNumber,
                c.Status.ToString(),
                db.Messages.Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.Id)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                c.LastMessageAt,
                c.AssignedUserId,
                db.Messages.Count(m =>
                    m.ConversationId == c.Id &&
                    m.SenderType == SenderType.Customer &&
                    (c.LastReadAt == null || m.Timestamp > c.LastReadAt)),
                c.HandledByAi,
                c.AiMode))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        return Result.Success(new PagedResult<ConversationListItemResponse>(rows, hasMore));
    }

    public async Task<Result<PagedResult<MessageResponse>>> GetMessagesAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, int? before, int pageSize, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<PagedResult<MessageResponse>>(access.Error);

        pageSize = Math.Clamp(pageSize, 1, 100);

        var messages = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && (before == null || m.Id < before))
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1)
            .Select(m => new MessageResponse(
                m.Id,
                m.SenderType.ToString(),
                m.Content,
                m.MessageType.ToString(),
                m.Status.ToString(),
                m.Timestamp,
                m.SenderUserId,
                m.MediaUrl,
                m.MediaMimeType,
                m.MediaFileName))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > pageSize;
        if (hasMore)
            messages.RemoveAt(messages.Count - 1);

        return Result.Success(new PagedResult<MessageResponse>(messages, hasMore));
    }

    public async Task<Result<ConversationDetailResponse>> GetDetailAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<ConversationDetailResponse>(access.Error);

        var detail = await db.Conversations.AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                c.Id,
                c.Customer.Name,
                c.Customer.PhoneNumber,
                c.Status,
                c.AssignedUserId,
                AssignedUserName = c.AssignedUser != null ? c.AssignedUser.Name : null,
                Tags = c.ConversationTags.Select(t => t.Tag.Name).ToList(),
                c.CreatedAt,
                c.LastMessageAt,
                // Prefer the persisted window anchor (set on every inbound); fall back to deriving it
                // from message history for conversations created before the window migration ran.
                LastInboundAt = c.LastCustomerMessageAt ?? db.Messages
                    .Where(m => m.ConversationId == c.Id && m.SenderType == SenderType.Customer)
                    .Max(m => (DateTime?)m.Timestamp),
                c.WindowExpiresAt,
                c.WindowType,
                MessageCount = db.Messages.Count(m => m.ConversationId == c.Id),
                c.GroupId,
                GroupName = c.Group != null ? c.Group.Name : null,
                c.CurrentStageId,
                CurrentStageName = c.CurrentStage != null ? c.CurrentStage.Name : null,
                c.HandledByAi,
                c.AiMode
            })
            .FirstOrDefaultAsync(cancellationToken);

		if (detail is null)
            return Result.Failure<ConversationDetailResponse>(AppErrors.ConversationNotFound);

        var allowed = ConversationStatusTransitions.AllowedNext(detail.Status).Select(s => s.ToString()).ToList();
        // Build a lightweight transient entity to drive the window service. No DB write.
        var transient = new Conversation
        {
            LastCustomerMessageAt = detail.LastInboundAt,
            WindowExpiresAt       = detail.WindowExpiresAt,
            WindowType            = detail.WindowType
        };
        var windowState = windowService.EvaluateConversation(transient);

        return Result.Success(new ConversationDetailResponse(
            detail.Id, detail.Name, detail.PhoneNumber, detail.Status.ToString(),
            allowed, detail.AssignedUserId, detail.AssignedUserName, detail.Tags,
            detail.CreatedAt, detail.LastMessageAt, detail.LastInboundAt,
            windowState.WindowExpiresAt, windowState.IsOpen, windowState.WindowType.ToString(), (long)windowState.RemainingTime.TotalSeconds, detail.MessageCount,
            detail.GroupId, detail.GroupName, detail.CurrentStageId, detail.CurrentStageName,
            detail.HandledByAi, detail.AiMode));
    }

    public async Task<Result<ConversationStatusResponse>> ChangeStatusAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string targetStatus, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<ConversationStatusResponse>(access.Error);

        if (!Enum.TryParse<ConversationStatus>(targetStatus, ignoreCase: true, out var target))
            return Result.Failure<ConversationStatusResponse>(AppErrors.ConversationInvalidTransition);

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure<ConversationStatusResponse>(AppErrors.ConversationNotFound);

        if (conversation.Status != target && !ConversationStatusTransitions.CanTransition(conversation.Status, target))
            return Result.Failure<ConversationStatusResponse>(AppErrors.ConversationInvalidTransition);

        conversation.Status = target;
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await notifier.ConversationChangedAsync(tenantId!.Value, new ConversationChangedPayload(
            conversation.Id, conversation.Status.ToString(), conversation.AssignedUserId, conversation.LastMessageAt), cancellationToken);

        var allowed = ConversationStatusTransitions.AllowedNext(target).Select(s => s.ToString()).ToList();
        return Result.Success(new ConversationStatusResponse(conversation.Id, target.ToString(), allowed));
    }

    public async Task<Result<SendMessageResult>> SendTextAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string text, string? currentUserEmail = null, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<SendMessageResult>(access.Error);

        var phone = await db.Conversations
            .Where(c => c.Id == conversationId)
            .Select(c => c.Customer.PhoneNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure<SendMessageResult>(AppErrors.ConversationNotFound);

        // currentUserId is the Identity (GUID) subject mis-parsed as int (see ConversationsController);
        // resolve the real domain user id by email so we never insert a Message.SenderUserId that
        // doesn't exist in the users table (FK violation).
        int? senderUserId = string.IsNullOrEmpty(currentUserEmail)
            ? null
            : await db.Users.Where(u => u.TenantId == tenantId && u.Email == currentUserEmail)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

        return await whatsApp.SendTextAsync(tenantId, phone, text, senderUserId, WaslX.Domain.SharedEnums.SenderType.Agent, cancellationToken);
    }

    public async Task<Result> MarkReadAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return access;

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        conversation.LastReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return access;

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        conversation.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<SendMessageResult>> SendMediaAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId,
        byte[] fileContent, string fileName, string contentType, string? caption,
        string? currentUserEmail = null, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<SendMessageResult>(access.Error);

        var phone = await db.Conversations
            .Where(c => c.Id == conversationId)
            .Select(c => c.Customer.PhoneNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure<SendMessageResult>(AppErrors.ConversationNotFound);

        var uploadResult = await mediaStorage.UploadAsync(fileContent, fileName, contentType, cancellationToken);
        if (uploadResult.IsFailure)
            return Result.Failure<SendMessageResult>(uploadResult.Error);

        // WhatsApp only accepts webp as a sticker (not as an image), so route webp files accordingly.
        var mediaType = contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase) ? "sticker"
            : contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image"
            : contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video"
            : "document";

        int? senderUserId = string.IsNullOrEmpty(currentUserEmail)
            ? null
            : await db.Users.Where(u => u.TenantId == tenantId && u.Email == currentUserEmail)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

        return await whatsApp.SendMediaAsync(
            tenantId, phone, mediaType, uploadResult.Value.Url, caption, fileName, contentType, senderUserId, WaslX.Domain.SharedEnums.SenderType.Agent, cancellationToken);
    }

    public async Task<Result> ChangeAiModeAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string modeStr, CancellationToken cancellationToken = default)
    {
        if (tenantId is null)
            return Result.Failure(AppErrors.NoTenantContext);

        if (!Enum.TryParse<WaslX.Domain.SharedEnums.AiConversationMode>(modeStr, true, out var parsedMode) || parsedMode == 0)
            return Result.Failure(AppErrors.InvalidStatus);

        var conversation = await db.Conversations
            .Include(c => c.WhatsAppAccount)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);

        if (conversation is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        if (!isPrivileged && conversation.AssignedUserId != currentUserId)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        // Check if Admin disabled AI for this number.
        // Priority: per-number record (if it exists) → tenant-level setting as fallback.
        var numberSettings = await db.AiAgentNumberSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WhatsAppAccountId == conversation.WhatsAppAccountId, cancellationToken);

        bool aiEnabled;
        if (numberSettings is not null)
        {
            aiEnabled = numberSettings.Enabled;
        }
        else
        {
            // No per-number override: fall back to the tenant-level toggle.
            var tenantSettings = await db.TenantAiAgentSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
            aiEnabled = tenantSettings?.Enabled ?? false;
        }

        if (!aiEnabled)
            return Result.Failure(AppErrors.AiNumberDisabled);

        // Update the conversation AI Mode
        conversation.AiMode = parsedMode;
        
        await db.SaveChangesAsync(cancellationToken);

        // Broadcast to clients
        await notifier.ConversationAiModeChangedAsync(tenantId.Value, new ConversationAiModeChangedPayload(
            conversation.Id, conversation.AiMode.ToString()), cancellationToken);

        await notifier.ConversationChangedAsync(tenantId.Value, new ConversationChangedPayload(
            conversation.Id,
            conversation.Status.ToString(),
            conversation.AssignedUserId,
            conversation.LastMessageAt,
            conversation.HandledByAi,
            conversation.AiMode.ToString()), cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Applies the optional US-3.9 filters to an already tenant- and role-scoped conversation query.
    /// Every clause is additive (AND) and only runs when its input is provided, so a null / all-null
    /// filter returns the query untouched (identical default inbox behavior). Free-text search matches
    /// customer name, customer phone and message content — the message join stays tenant-scoped because
    /// it is keyed on the conversation, which is already restricted to this tenant.
    /// </summary>
    private IQueryable<Conversation> ApplyFilter(IQueryable<Conversation> query, ConversationFilter? filter, int? resolvedAssignedUserId)
    {
        if (filter is null)
            return query;

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<ConversationStatus>(filter.Status, ignoreCase: true, out var status))
            query = query.Where(c => c.Status == status);

        if (filter.Unassigned == true)
            query = query.Where(c => c.AssignedUserId == null);
        else if (resolvedAssignedUserId is { } assignedUserId)
            query = query.Where(c => c.AssignedUserId == assignedUserId);

        if (filter.GroupId is { } groupId)
            query = query.Where(c => c.GroupId == groupId);

        if (filter.WhatsAppAccountId is { } waAccountId)
            query = query.Where(c => c.WhatsAppAccountId == waAccountId);

        if (filter.CustomerId is { } customerId)
            query = query.Where(c => c.CustomerId == customerId);

        if (filter.TagId is { } tagId)
            query = query.Where(c => c.ConversationTags.Any(t => t.TagId == tagId));

        if (filter.DateFrom is { } dateFrom)
            query = query.Where(c => c.LastMessageAt >= dateFrom);

        if (filter.DateTo is { } dateTo)
            query = query.Where(c => c.LastMessageAt <= dateTo);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var like = $"%{filter.Search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Customer.Name, like) ||
                EF.Functions.Like(c.Customer.PhoneNumber, like) ||
                db.Messages.Any(m => m.ConversationId == c.Id && EF.Functions.Like(m.Content, like)));
        }

        return query;
    }

    /// <summary>Tenant-scopes and RBAC-checks a conversation: managers/admins may access any; agents only their own.</summary>
    private async Task<Result> ResolveConversationAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conv = await db.Conversations.AsNoTracking()
            .Where(c => c.Id == conversationId && c.TenantId == tid && !c.IsDeleted)
            .Select(c => new { c.AssignedUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (conv is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        if (!isPrivileged && conv.AssignedUserId != currentUserId)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        return Result.Success();
    }
}
