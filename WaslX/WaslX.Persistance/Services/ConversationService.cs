using Microsoft.EntityFrameworkCore;
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
    IInboxRealtimeNotifier notifier) : IConversationService
{
    public async Task<Result<PagedResult<ConversationListItemResponse>>> GetConversationsAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<PagedResult<ConversationListItemResponse>>(AppErrors.NoTenantContext);

        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Conversations.AsNoTracking().Where(c => c.TenantId == tid && !c.IsDeleted);

        // Agents see only their own assignments; managers/admins see the whole tenant.
        if (!isPrivileged)
            query = query.Where(c => c.AssignedUserId == currentUserId);

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
                    (c.LastReadAt == null || m.Timestamp > c.LastReadAt))))
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
                c.Customer.VipFlag,
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
                WindowExpiresAt = c.ServiceWindowExpiresAt,
                MessageCount = db.Messages.Count(m => m.ConversationId == c.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

		if (detail is null)
            return Result.Failure<ConversationDetailResponse>(AppErrors.ConversationNotFound);

        var allowed = ConversationStatusTransitions.AllowedNext(detail.Status).Select(s => s.ToString()).ToList();
        var isWindowOpen = detail.WindowExpiresAt is not null && DateTime.UtcNow < detail.WindowExpiresAt;
        return Result.Success(new ConversationDetailResponse(
            detail.Id, detail.Name, detail.PhoneNumber, detail.VipFlag, detail.Status.ToString(),
            allowed, detail.AssignedUserId, detail.AssignedUserName, detail.Tags,
            detail.CreatedAt, detail.LastMessageAt, detail.LastInboundAt,
            detail.WindowExpiresAt, isWindowOpen, detail.MessageCount));
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

        return await whatsApp.SendTextAsync(tenantId, phone, text, senderUserId, cancellationToken);
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
            tenantId, phone, mediaType, uploadResult.Value.Url, caption, fileName, contentType, senderUserId, cancellationToken);
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
