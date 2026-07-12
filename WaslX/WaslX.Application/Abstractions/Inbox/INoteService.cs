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

    /// <param name="currentUserEmail">Resolves the note's author domain <c>User.Id</c> (the JWT subject is the Identity GUID).</param>
    Task<Result<NoteDto>> AddNoteAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string content, string? currentUserEmail, CancellationToken cancellationToken = default);
}
