using Microsoft.AspNetCore.SignalR;
using WaslX.Api.Hubs;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Application.Features.Escalation.Screening;
using System.Threading;
using System.Threading.Tasks;

namespace WaslX.Api.Realtime;

/// <summary>
/// <see cref="IInboxRealtimeNotifier"/> over SignalR. Broadcasts to the tenant group so every
/// connected agent/manager of that workspace receives the change; the client reconciles against
/// the server-filtered read endpoints, so a push never widens what a caller is allowed to see.
/// </summary>
internal sealed class InboxRealtimeNotifier(IHubContext<InboxHub> hub) : IInboxRealtimeNotifier
{
    public Task MessageReceivedAsync(int tenantId, InboxMessagePayload message, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("MessageReceived", message, cancellationToken);

    public Task MessageStatusChangedAsync(int tenantId, int conversationId, int messageId, string status, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId))
            .SendAsync("MessageStatusChanged", new { conversationId, messageId, status }, cancellationToken);

    public Task ConversationChangedAsync(int tenantId, ConversationChangedPayload change, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("ConversationChanged", change, cancellationToken);

    public Task NoteAddedAsync(int tenantId, InboxNotePayload note, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("NoteAdded", note, cancellationToken);

    public Task NotificationCreatedAsync(int tenantId, int userId, object payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId))
            .SendAsync("NotificationCreated", new { userId, notification = payload }, cancellationToken);

    public Task MessageClassificationUpdatedAsync(int tenantId, MessageClassificationPayload payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("MessageClassificationUpdated", payload, cancellationToken);

    public Task EscalationRecommendationUpdatedAsync(int tenantId, EscalationScoringResult result, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("EscalationRecommendationUpdated", result, cancellationToken);

    public Task EscalationAssignmentConfirmedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("EscalationAssignmentConfirmed", result, cancellationToken);

    public Task EscalationOverrideAppliedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("EscalationOverrideApplied", result, cancellationToken);

    public Task EscalationAutoAssignedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("EscalationAutoAssigned", result, cancellationToken);

    public Task ConversationOwnershipTransferredAsync(int tenantId, OwnershipTransferredPayload payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("ConversationOwnershipTransferred", payload, cancellationToken);

    public Task ConversationEscalatedAsync(int tenantId, ConversationEscalatedPayload payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("ConversationEscalated", payload, cancellationToken);

    public Task ConversationTakenOverAsync(int tenantId, ConversationTakenOverPayload payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("ConversationTakenOver", payload, cancellationToken);

    public Task ConversationAiModeChangedAsync(int tenantId, ConversationAiModeChangedPayload payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("ConversationAiModeChanged", payload, cancellationToken);

    public Task EscalationRejectedAsync(int tenantId, EscalationRecommendation result, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(InboxHub.TenantGroup(tenantId)).SendAsync("EscalationRejected", result, cancellationToken);
}
