using WaslX.Application.Features.Escalation.Models;
using WaslX.Application.Features.Escalation.Screening;

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

    /// <summary>A message was classified (topic/sentiment/priority/escalate) by the AI screening pipeline.</summary>
    Task MessageClassificationUpdatedAsync(int tenantId, MessageClassificationPayload payload, CancellationToken cancellationToken = default);

    /// <summary>An escalation's suggested-assignee recommendation was computed or recomputed.</summary>
    Task EscalationRecommendationUpdatedAsync(int tenantId, EscalationScoringResult result, CancellationToken cancellationToken = default);

    /// <summary>A Manager/Admin confirmed a recommended escalation assignment.</summary>
    Task EscalationAssignmentConfirmedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default);

    /// <summary>A Manager/Admin overrode the suggested assignee for an escalation.</summary>
    Task EscalationOverrideAppliedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default);

    /// <summary>An escalation was auto-assigned (autoAssign mode, no human confirmation needed).</summary>
    Task EscalationAutoAssignedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default);

    /// <summary>A conversation's assigned owner changed (confirm / override / auto-assign).</summary>
    Task ConversationOwnershipTransferredAsync(int tenantId, OwnershipTransferredPayload payload, CancellationToken cancellationToken = default);

    /// <summary>US-4.4: a conversation was auto-escalated; notifies Managers/Admins.</summary>
    Task ConversationEscalatedAsync(int tenantId, ConversationEscalatedPayload payload, CancellationToken cancellationToken = default);

    /// <summary>An agent took over a conversation the AI was handling.</summary>
    Task ConversationTakenOverAsync(int tenantId, ConversationTakenOverPayload payload, CancellationToken cancellationToken = default);

    /// <summary>A conversation's AI mode (Active/Paused/etc.) changed.</summary>
    Task ConversationAiModeChangedAsync(int tenantId, ConversationAiModeChangedPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>Realtime payload for AI takeover events.</summary>
public record ConversationTakenOverPayload(int ConversationId, DateTime OccurredAtUtc);

/// <summary>Realtime payload for ownership transfer events.</summary>
public record OwnershipTransferredPayload(
    int ConversationId,
    int? PreviousOwnerId,
    int NewOwnerId,
    string TransitionType,
    DateTime OccurredAtUtc,
    DateTime? OwnershipTransferredAtUtc);

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
    DateTime? LastMessageAt,
    bool HandledByAi = false,
    string AiMode = "Active");

/// <summary>Realtime projection of an AI mode change.</summary>
public record ConversationAiModeChangedPayload(
    int ConversationId,
    string AiMode);

/// <summary>Realtime projection of an internal note.</summary>
public record InboxNotePayload(
    int Id,
    int ConversationId,
    string Content,
    string AuthorName,
    DateTime CreatedAt);

/// <summary>US-4.4: Auto-escalation triggered payload.</summary>
public record ConversationEscalatedPayload(
    int EscalationId,
    int ConversationId,
    int TenantId,
    string Reason,
    string Priority,
    string Sentiment,
    string Status,
    DateTime OccurredAtUtc);

/// <summary>Realtime projection of a message classification result.</summary>
public record MessageClassificationPayload(
    int ConversationId,
    int MessageId,
    MessageClassificationDto Classification);

public record MessageClassificationDto(
    string Topic,
    string Language,
    string Sentiment,
    string Priority,
    bool Escalate,
    string Reason);
