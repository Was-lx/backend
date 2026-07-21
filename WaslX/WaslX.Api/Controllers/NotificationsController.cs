using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Notifications;

namespace WaslX.Api.Controllers;

/// <summary>In-app notifications for the signed-in user (any role).</summary>
[ApiController]
[Authorize]
public class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet("api/notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetNotificationsQuery(User.GetTenantId(), User.GetDomainUserId(), unreadOnly, take), cancellationToken)).ToActionResult();

    [HttpGet("api/notifications/unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken) =>
        (await sender.Send(new GetUnreadCountQuery(User.GetTenantId(), User.GetDomainUserId()), cancellationToken)).ToActionResult();

    [HttpPost("api/notifications/{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new MarkNotificationReadCommand(User.GetTenantId(), User.GetDomainUserId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/notifications/read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken) =>
        (await sender.Send(new MarkAllNotificationsReadCommand(User.GetTenantId(), User.GetDomainUserId()), cancellationToken)).ToActionResult();
}
