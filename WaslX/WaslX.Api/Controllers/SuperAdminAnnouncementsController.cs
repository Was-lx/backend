using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Platform-wide announcements (US-6.10b). Create as a draft, then publish to broadcast an in-app
/// notification (over SignalR) to the owner/Admin of every targeted tenant. Guarded by the SuperAdmin
/// role. Every mutation is written to the global platform audit trail.
/// </summary>
[ApiController]
[Route("api/superadmin/announcements")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminAnnouncementsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetAnnouncementsQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateAnnouncementCommand(input, Actor()), cancellationToken)).ToActionResult();

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new PublishAnnouncementCommand(id, Actor()), cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteAnnouncementCommand(id, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
