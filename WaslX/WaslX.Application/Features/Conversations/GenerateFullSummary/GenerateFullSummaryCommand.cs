using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.GenerateFullSummary;

/// <summary>Generates the full structured summary (key points · decisions · what's needed) on demand.</summary>
public record GenerateFullSummaryCommand(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId)
    : ICommand<ConversationSummaryResponse>;

public class GenerateFullSummaryCommandHandler(IConversationSummaryService summaries)
    : ICommandHandler<GenerateFullSummaryCommand, ConversationSummaryResponse>
{
    public Task<Result<ConversationSummaryResponse>> Handle(GenerateFullSummaryCommand request, CancellationToken cancellationToken) =>
        summaries.GenerateFullAsync(request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, cancellationToken);
}
