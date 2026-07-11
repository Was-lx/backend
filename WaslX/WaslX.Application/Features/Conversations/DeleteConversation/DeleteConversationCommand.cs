using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.DeleteConversation;

/// <summary>Soft-deletes a conversation from the shared inbox.</summary>
public record DeleteConversationCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId)
    : ICommand;

public class DeleteConversationCommandHandler(IConversationService conversations)
    : ICommandHandler<DeleteConversationCommand>
{
    public Task<Result> Handle(DeleteConversationCommand request, CancellationToken cancellationToken) =>
        conversations.DeleteAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, cancellationToken);
}
