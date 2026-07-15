using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WaslX.Api.Extensions;
using WaslX.Application.Abstractions.Presence;

namespace WaslX.Api.Hubs;

/// <summary>
/// Real-time shared-inbox hub. On connect, the caller joins their tenant group so every
/// inbox change for that workspace is pushed to them. Optional per-conversation groups let a
/// client scope to the thread it currently has open. Connect/disconnect also drives the agent's
/// online presence (used by Round Robin distribution).
/// </summary>
[Authorize]
public sealed class InboxHub(IPresenceService presence, ILogger<InboxHub> logger) : Hub
{
    internal static string TenantGroup(int tenantId) => $"tenant-{tenantId}";
    internal static string ConversationGroup(int conversationId) => $"conversation-{conversationId}";

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.GetTenantId();
        if (tenantId is { } tid)
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tid));

        // Presence is a best-effort side-effect: never let it abort the hub connection.
        try
        {
            await presence.SetOnlineAsync(Context.User?.GetTenantId(), Context.User?.GetEmail(), Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark agent online on connect for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Presence is a best-effort side-effect: never let it abort the disconnect handling.
        try
        {
            await presence.SetOfflineAsync(Context.User?.GetTenantId(), Context.User?.GetEmail());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark agent offline on disconnect for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Scopes the connection to a single conversation thread (in addition to the tenant group).</summary>
    public Task JoinConversation(int conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

    public Task LeaveConversation(int conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
}
