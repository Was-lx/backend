using WaslX.Application.Features.Notes.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Inbox;

/// <summary>
/// Internal team notes on a conversation — tenant-scoped and RBAC-checked exactly like the
/// conversation itself. Notes are visible only to the team and are NEVER sent to the customer
/// (no Meta Graph call is ever made for a note).
/// </summary>
public interface INoteService
{
    Task<Result<IReadOnlyList<NoteDto>>> GetNotesAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default);

    /// <param name="authorName">Display name of the author (from the JWT), used when mirroring the Identity user into a domain user row.</param>
    /// <param name="roleName">The author's role name (from the JWT), used for the mirrored domain user's required role.</param>
    /// <param name="currentUserEmail">Identifies (and, if missing, creates) the author's domain <c>User</c> row by tenant + email.</param>
    Task<Result<NoteDto>> AddNoteAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string content, string authorName, string roleName, string? currentUserEmail, CancellationToken cancellationToken = default);
}
