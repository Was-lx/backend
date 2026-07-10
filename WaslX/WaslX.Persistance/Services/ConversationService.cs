using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class ConversationService(ApplicationDbContext db, IWhatsAppService whatsApp) : IConversationService
{
    public async Task<Result<PagedResult<ConversationListItemResponse>>> GetConversationsAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<PagedResult<ConversationListItemResponse>>(AppErrors.NoTenantContext);

        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Conversations.AsNoTracking().Where(c => c.TenantId == tid);

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
                c.AssignedUserId))
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
                m.SenderUserId))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > pageSize;
        if (hasMore)
            messages.RemoveAt(messages.Count - 1);

        return Result.Success(new PagedResult<MessageResponse>(messages, hasMore));
    }

    public async Task<Result<SendMessageResult>> SendTextAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string text, CancellationToken cancellationToken = default)
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

        return await whatsApp.SendTextAsync(tenantId, phone, text, currentUserId, cancellationToken);
    }

    /// <summary>Tenant-scopes and RBAC-checks a conversation: managers/admins may access any; agents only their own.</summary>
    private async Task<Result> ResolveConversationAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var conv = await db.Conversations.AsNoTracking()
            .Where(c => c.Id == conversationId && c.TenantId == tid)
            .Select(c => new { c.AssignedUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (conv is null)
            return Result.Failure(AppErrors.ConversationNotFound);

        if (!isPrivileged && conv.AssignedUserId != currentUserId)
            return Result.Failure(AppErrors.ConversationAccessDenied);

        return Result.Success();
    }
}
