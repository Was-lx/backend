using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Conversations.GetConversationMessages;

/// <summary>Cursor-paginated message history for one conversation (RBAC-checked, tenant-scoped).</summary>
public record GetConversationMessagesQuery(int? TenantId, int CurrentUserId, bool IsPrivileged, int ConversationId, int? Before, int PageSize)
    : IQuery<PagedResult<MessageResponse>>;

public class GetConversationMessagesQueryHandler(IConversationService conversations)
    : IQueryHandler<GetConversationMessagesQuery, PagedResult<MessageResponse>>
{
    public Task<Result<PagedResult<MessageResponse>>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken) =>
        conversations.GetMessagesAsync(
            request.TenantId, request.CurrentUserId, request.IsPrivileged, request.ConversationId, request.Before, request.PageSize, cancellationToken);
}
