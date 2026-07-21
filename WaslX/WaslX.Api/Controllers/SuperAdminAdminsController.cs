using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Platform-owner management of SuperAdmin users (US-6.1). Cross-tenant by nature; every mutation is
/// audited via <c>IPlatformAuditService</c> with the calling SuperAdmin as the actor.
/// </summary>
[ApiController]
[Route("api/superadmin/admins")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminAdminsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetSuperAdminsQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSuperAdminInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateSuperAdminCommand(input, Actor()), cancellationToken)).ToActionResult();

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetSuperAdminStatusInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new SetSuperAdminStatusCommand(id, input.IsDisabled, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
