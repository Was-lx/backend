using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Notes.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class NoteService(ApplicationDbContext db, IInboxRealtimeNotifier notifier) : INoteService
{
    public async Task<Result<IReadOnlyList<NoteDto>>> GetNotesAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<IReadOnlyList<NoteDto>>(access.Error);

        var notes = await db.InternalNotes.AsNoTracking()
            .Where(n => n.ConversationId == conversationId)
            .OrderBy(n => n.Id)
            .Select(n => new NoteDto(n.Id, n.ConversationId, n.Content, n.User.Name, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<NoteDto>>(notes);
    }

    public async Task<Result<NoteDto>> AddNoteAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string content, string? currentUserEmail, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<NoteDto>(access.Error);

        // InternalNote.UserId is a non-nullable FK, so we must attribute the note to a real domain
        // user. The JWT subject is the Identity GUID, not the domain int id — resolve it by email.
        var author = string.IsNullOrEmpty(currentUserEmail)
            ? null
            : await db.Users.Where(u => u.TenantId == tenantId && u.Email == currentUserEmail)
                .Select(u => new { u.Id, u.Name })
                .FirstOrDefaultAsync(cancellationToken);

        if (author is null)
            return Result.Failure<NoteDto>(AppErrors.UserContextNotResolved);

        var note = new InternalNote
        {
            ConversationId = conversationId,
            UserId = author.Id,
            Content = content
        };
        await db.InternalNotes.AddAsync(note, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new NoteDto(note.Id, note.ConversationId, note.Content, author.Name, note.CreatedAt);
        await notifier.NoteAddedAsync(tenantId!.Value, new InboxNotePayload(note.Id, note.ConversationId, note.Content, author.Name, note.CreatedAt), cancellationToken);
        return Result.Success(dto);
    }

    /// <summary>Tenant-scopes and RBAC-checks a conversation: managers/admins any; agents only their own.</summary>
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
