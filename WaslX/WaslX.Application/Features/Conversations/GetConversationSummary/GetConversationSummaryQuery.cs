using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.GetConversationSummary;

/// <summary>Returns the cached one-line summary for a conversation, generating/refreshing it if needed.</summary>
public record GetConversationSummaryQuery(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId)
    : IQuery<ConversationSummaryResponse>;

public class GetConversationSummaryQueryHandler(IConversationSummaryService summaries)
    : IQueryHandler<GetConversationSummaryQuery, ConversationSummaryResponse>
{
    public Task<Result<ConversationSummaryResponse>> Handle(GetConversationSummaryQuery request, CancellationToken cancellationToken) =>
        summaries.GetOrCreateShortAsync(request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, cancellationToken);
}
