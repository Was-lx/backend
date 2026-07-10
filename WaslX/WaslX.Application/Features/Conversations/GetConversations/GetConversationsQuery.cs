using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.GetConversations;

/// <summary>Lists shared-inbox conversations for the caller (role-filtered, tenant-scoped, paginated).</summary>
public record GetConversationsQuery(int? TenantId, int CurrentUserId, bool IsPrivileged, int Page, int PageSize)
    : IQuery<PagedResult<ConversationListItemResponse>>;

public class GetConversationsQueryHandler(IConversationService conversations)
    : IQueryHandler<GetConversationsQuery, PagedResult<ConversationListItemResponse>>
{
    public Task<Result<PagedResult<ConversationListItemResponse>>> Handle(GetConversationsQuery request, CancellationToken cancellationToken) =>
        conversations.GetConversationsAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.Page, request.PageSize, cancellationToken);
}
