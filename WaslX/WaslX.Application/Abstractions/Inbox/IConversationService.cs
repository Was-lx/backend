using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Application.Features.WhatsApp.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Inbox;

/// <summary>
/// Shared-inbox read + reply operations, tenant-scoped and role-filtered. Owns direct
/// DbContext access for conversation/message queries; delegates the actual WhatsApp send
/// to <see cref="WaslX.Application.Abstractions.WhatsApp.IWhatsAppService"/>.
/// </summary>
public interface IConversationService
{
    /// <param name="isPrivileged">Manager/Admin see all tenant conversations; agents see only their own assignments.</param>
    Task<Result<PagedResult<ConversationListItemResponse>>> GetConversationsAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<MessageResponse>>> GetMessagesAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, int? before, int pageSize, CancellationToken cancellationToken = default);

    /// <param name="currentUserEmail">
    /// Used to resolve the sending domain <c>User.Id</c> for message attribution, since the JWT's
    /// subject is the Identity (GUID) user id, not the domain int id (see <paramref name="currentUserId"/>).
    /// </param>
    Task<Result<SendMessageResult>> SendTextAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, string text, string? currentUserEmail = null, CancellationToken cancellationToken = default);
}
