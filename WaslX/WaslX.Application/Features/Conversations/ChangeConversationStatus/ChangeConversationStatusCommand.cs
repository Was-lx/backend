using FluentValidation;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.ChangeConversationStatus;

/// <summary>Applies a manual conversation lifecycle transition (state machine enforced server-side).</summary>
public record ChangeConversationStatusCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId, string Status)
    : ICommand<ConversationStatusResponse>;

public class ChangeConversationStatusCommandHandler(IConversationService conversations)
    : ICommandHandler<ChangeConversationStatusCommand, ConversationStatusResponse>
{
    public Task<Result<ConversationStatusResponse>> Handle(ChangeConversationStatusCommand request, CancellationToken cancellationToken) =>
        conversations.ChangeStatusAsync(request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, request.Status, cancellationToken);
}

public class ChangeConversationStatusCommandValidator : AbstractValidator<ChangeConversationStatusCommand>
{
    public ChangeConversationStatusCommandValidator()
    {
        RuleFor(x => x.Status).NotEmpty().MaximumLength(20);
    }
}
