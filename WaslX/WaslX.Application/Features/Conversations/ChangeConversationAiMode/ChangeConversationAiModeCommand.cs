using MediatR;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.ChangeConversationAiMode;

public record ChangeConversationAiModeCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId, string Mode) 
    : ICommand;

public class ChangeConversationAiModeCommandHandler(IConversationService conversations) 
    : ICommandHandler<ChangeConversationAiModeCommand>
{
    public Task<Result> Handle(ChangeConversationAiModeCommand request, CancellationToken cancellationToken) =>
        conversations.ChangeAiModeAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, request.Mode, cancellationToken);
}
