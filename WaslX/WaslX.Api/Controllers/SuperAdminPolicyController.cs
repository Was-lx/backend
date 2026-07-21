using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Global platform policy (US-6.7): data retention, API rate limit, default routing mode. Guarded by the
/// SuperAdmin role. Global — no tenant scope.
/// </summary>
[ApiController]
[Route("api/superadmin/policy")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminPolicyController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await sender.Send(new GetPlatformPolicyQuery(), cancellationToken)).ToActionResult();

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] SetPlatformPolicyInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new SetPlatformPolicyCommand(input, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
