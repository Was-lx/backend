using FluentValidation;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.SendConversationMessage;

/// <summary>Sends a text reply within an existing conversation, attributed to the sending user.</summary>
public record SendConversationMessageCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId, string Text)
    : ICommand<SendMessageResult>;

public class SendConversationMessageCommandHandler(IConversationService conversations)
    : ICommandHandler<SendConversationMessageCommand, SendMessageResult>
{
    public Task<Result<SendMessageResult>> Handle(SendConversationMessageCommand request, CancellationToken cancellationToken) =>
        conversations.SendTextAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, request.Text, cancellationToken);
}

public class SendConversationMessageCommandValidator : AbstractValidator<SendConversationMessageCommand>
{
    public SendConversationMessageCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4096);
    }
}
