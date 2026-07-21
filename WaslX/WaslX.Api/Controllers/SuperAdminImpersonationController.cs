using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Audited SuperAdmin impersonation (US-6.8). Starting a session mints a short-lived tenant JWT that lets
/// the platform owner act inside the target workspace; ending it closes the session. Guarded by the
/// SuperAdmin role. Both start and end are written to the global platform audit trail.
/// </summary>
[ApiController]
[Route("api/superadmin/impersonate")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminImpersonationController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartImpersonationInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new StartImpersonationCommand(input, Actor()), cancellationToken)).ToActionResult();

    [HttpPost("{sessionId:int}/end")]
    public async Task<IActionResult> End(int sessionId, CancellationToken cancellationToken) =>
        (await sender.Send(new EndImpersonationCommand(sessionId, Actor()), cancellationToken)).ToActionResult();

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken) =>
        (await sender.Send(new GetActiveImpersonationsQuery(), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
