using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Global platform credential / secret management (US-6.6). Guarded by the SuperAdmin role. Reads are
/// always MASKED — the raw and encrypted values never leave the server, and are never exposed to tenants.
/// </summary>
[ApiController]
[Route("api/superadmin/credentials")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminCredentialsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetPlatformCredentialsQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlatformCredentialInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new CreatePlatformCredentialCommand(input, Actor()), cancellationToken)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePlatformCredentialInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdatePlatformCredentialCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    [HttpPost("{id:int}/rotate")]
    public async Task<IActionResult> Rotate(int id, [FromBody] RotatePlatformCredentialInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new RotatePlatformCredentialCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeletePlatformCredentialCommand(id, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
