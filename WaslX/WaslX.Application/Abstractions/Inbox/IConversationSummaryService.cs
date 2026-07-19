using WaslX.Application.Features.Conversations.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Inbox;

/// <summary>
/// AI conversation summarization. Tenant-scoped + RBAC-gated exactly like the shared inbox.
/// A concise one-line summary is available on demand for any conversation and cached; a longer
/// structured summary is generated only when requested. Both are refreshed as new messages arrive.
/// </summary>
public interface IConversationSummaryService
{
    /// <summary>
    /// Returns the cached one-line summary, generating (or refreshing) it if absent or stale.
    /// </summary>
    Task<Result<ConversationSummaryResponse>> GetOrCreateShortAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// (Re)generates the full structured summary (key points · decisions · what's needed) on demand.
    /// </summary>
    Task<Result<ConversationSummaryResponse>> GenerateFullAsync(
        int? tenantId, int currentUserId, bool isPrivileged, int conversationId, CancellationToken cancellationToken = default);
}
