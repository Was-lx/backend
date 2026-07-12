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
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string content, string authorName, string roleName, string? currentUserEmail, CancellationToken cancellationToken = default)
    {
        var access = await ResolveConversationAsync(tenantId, currentUserId, isPrivileged, conversationId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<NoteDto>(access.Error);

        // InternalNote.UserId is a non-nullable FK to the domain users table, but the app authenticates
        // via ASP.NET Identity (AspNetUsers) and the domain users table isn't populated for every
        // principal. Mirror the authenticated Identity user into a domain user row on first use so the
        // note can be attributed to a real row (find-or-create by tenant + email).
        var author = await ResolveOrCreateAuthorAsync(tenantId!.Value, currentUserEmail, authorName, roleName, cancellationToken);

        var note = new InternalNote
        {
            ConversationId = conversationId,
            UserId = author.Id,
            Content = content
        };
        await db.InternalNotes.AddAsync(note, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new NoteDto(note.Id, note.ConversationId, note.Content, author.Name, note.CreatedAt);
        await notifier.NoteAddedAsync(tenantId.Value, new InboxNotePayload(note.Id, note.ConversationId, note.Content, author.Name, note.CreatedAt), cancellationToken);
        return Result.Success(dto);
    }

    /// <summary>
    /// Returns the domain <see cref="User"/> that mirrors the authenticated Identity user, creating it
    /// (and its domain role, since users.RoleId is a required FK) the first time it's needed.
    /// </summary>
    private async Task<(int Id, string Name)> ResolveOrCreateAuthorAsync(
        int tenantId, string? email, string authorName, string roleName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(email))
        {
            var existing = await db.Users
                .Where(u => u.TenantId == tenantId && u.Email == email)
                .Select(u => new { u.Id, u.Name })
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null)
                return (existing.Id, existing.Name);
        }

        var effectiveRole = string.IsNullOrWhiteSpace(roleName) ? "Agent" : roleName;
        var roleId = await db.Set<Role>()
            .Where(r => r.Name == effectiveRole)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId is null)
        {
            var role = new Role { Name = effectiveRole, Description = effectiveRole };
            await db.Set<Role>().AddAsync(role, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            roleId = role.Id;
        }

        var displayName = string.IsNullOrWhiteSpace(authorName) ? (email ?? "User") : authorName;
        var user = new User
        {
            TenantId = tenantId,
            RoleId = roleId.Value,
            Name = displayName,
            Email = email ?? string.Empty,
            PasswordHash = string.Empty,
            Status = "Active"
        };
        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (user.Id, user.Name);
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
