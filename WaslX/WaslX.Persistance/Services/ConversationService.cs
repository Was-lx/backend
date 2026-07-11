using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class ConversationService(ApplicationDbContext db, IWhatsAppService whatsApp, IMediaStorageService mediaStorage) : IConversationService
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

        var mediaType = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image"
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
