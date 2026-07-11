using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.MarkConversationRead;

/// <summary>Advances a conversation's read-cursor so its unread count resets (internal only).</summary>
public record MarkConversationReadCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId)
    : ICommand;

public class MarkConversationReadCommandHandler(IConversationService conversations)
    : ICommandHandler<MarkConversationReadCommand>
{
    public Task<Result> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken) =>
        conversations.MarkReadAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, cancellationToken);
}
