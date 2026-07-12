using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Notes.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Notes.GetNotes;

/// <summary>Lists a conversation's internal team notes, oldest first.</summary>
public record GetNotesQuery(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId)
    : IQuery<IReadOnlyList<NoteDto>>;

public class GetNotesQueryHandler(INoteService notes) : IQueryHandler<GetNotesQuery, IReadOnlyList<NoteDto>>
{
    public Task<Result<IReadOnlyList<NoteDto>>> Handle(GetNotesQuery request, CancellationToken cancellationToken) =>
        notes.GetNotesAsync(request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, cancellationToken);
}
