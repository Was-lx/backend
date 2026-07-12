using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.GetConversationDetail;

/// <summary>Rich detail for the customer-context panel + status controls.</summary>
public record GetConversationDetailQuery(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId)
    : IQuery<ConversationDetailResponse>;

public class GetConversationDetailQueryHandler(IConversationService conversations)
    : IQueryHandler<GetConversationDetailQuery, ConversationDetailResponse>
{
    public Task<Result<ConversationDetailResponse>> Handle(GetConversationDetailQuery request, CancellationToken cancellationToken) =>
        conversations.GetDetailAsync(request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, cancellationToken);
}
