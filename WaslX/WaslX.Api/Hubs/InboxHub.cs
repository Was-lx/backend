using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WaslX.Api.Extensions;

namespace WaslX.Api.Hubs;

/// <summary>
/// Real-time shared-inbox hub. On connect, the caller joins their tenant group so every
/// inbox change for that workspace is pushed to them. Optional per-conversation groups let a
/// client scope to the thread it currently has open.
/// </summary>
[Authorize]
public sealed class InboxHub : Hub
{
    internal static string TenantGroup(int tenantId) => $"tenant-{tenantId}";
    internal static string ConversationGroup(int conversationId) => $"conversation-{conversationId}";

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.GetTenantId();
        if (tenantId is { } tid)
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tid));

        await base.OnConnectedAsync();
    }

    /// <summary>Scopes the connection to a single conversation thread (in addition to the tenant group).</summary>
    public Task JoinConversation(int conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

    public Task LeaveConversation(int conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
}
