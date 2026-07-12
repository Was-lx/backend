using FluentValidation;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Notes.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Notes.AddNote;

/// <summary>Adds an internal team note to a conversation (team-only; never sent to the customer).</summary>
public record AddNoteCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId, string Content, string AuthorName, string RoleName, string? CurrentUserEmail = null)
    : ICommand<NoteDto>;

public class AddNoteCommandHandler(INoteService notes) : ICommandHandler<AddNoteCommand, NoteDto>
{
    public Task<Result<NoteDto>> Handle(AddNoteCommand request, CancellationToken cancellationToken) =>
        notes.AddNoteAsync(request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, request.Content, request.AuthorName, request.RoleName, request.CurrentUserEmail, cancellationToken);
}

public class AddNoteCommandValidator : AbstractValidator<AddNoteCommand>
{
    public AddNoteCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4096);
    }
}
