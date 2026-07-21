namespace WaslX.Application.Abstractions.Realtime;

/// <summary>
/// Pushes shared-inbox changes to connected clients (SignalR) so agents see new messages,
/// status/receipt changes, notes and lifecycle transitions without polling. Implemented in the
/// API layer over <c>IHubContext</c>; consumed by the (Persistence) services that own the writes.
/// Events are broadcast to the tenant group — clients treat them as hints and reconcile against
/// the server-filtered read endpoints, so RBAC scoping is never widened by a push.
/// </summary>
public interface IInboxRealtimeNotifier
{
    /// <summary>A new inbound (customer) or outbound (agent) message was persisted.</summary>
    Task MessageReceivedAsync(int tenantId, InboxMessagePayload message, CancellationToken cancellationToken = default);

    /// <summary>A message's delivery/read status changed (queued/sent/delivered/read/failed).</summary>
    Task MessageStatusChangedAsync(int tenantId, int conversationId, int messageId, string status, CancellationToken cancellationToken = default);

    /// <summary>A conversation's status and/or assignment changed (including auto-reopen / first-reply advance).</summary>
    Task ConversationChangedAsync(int tenantId, ConversationChangedPayload change, CancellationToken cancellationToken = default);

    /// <summary>An internal team note was added to a conversation (never sent to the customer).</summary>
    Task NoteAddedAsync(int tenantId, InboxNotePayload note, CancellationToken cancellationToken = default);

    /// <summary>
    /// A new in-app notification was created for a specific user. Broadcast to the tenant group with
    /// the target <paramref name="userId"/> in the payload; the client shows it only if it is the recipient.
    /// </summary>
    Task NotificationCreatedAsync(int tenantId, int userId, object payload, CancellationToken cancellationToken = default);
}

/// <summary>Realtime projection of a message (mirrors the inbox MessageResponse DTO; no tokens).</summary>
public record InboxMessagePayload(
    int Id,
    int ConversationId,
    string SenderType,
    string Content,
    string MessageType,
    string Status,
    DateTime Timestamp,
    int? SenderUserId,
    string? MediaUrl,
    string? MediaMimeType,
    string? MediaFileName);

/// <summary>Realtime projection of a conversation status/assignment change.</summary>
public record ConversationChangedPayload(
    int ConversationId,
    string Status,
    int? AssignedUserId,
    DateTime? LastMessageAt);

/// <summary>Realtime projection of an internal note.</summary>
public record InboxNotePayload(
    int Id,
    int ConversationId,
    string Content,
    string AuthorName,
    DateTime CreatedAt);
