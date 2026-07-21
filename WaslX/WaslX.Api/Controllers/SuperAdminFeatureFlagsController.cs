using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Feature-flag management (US-6.7). Guarded by the SuperAdmin role. A null-tenant row is the global
/// default; per-tenant rows override it. Resolution honours per-tenant &gt; global &gt; off.
/// </summary>
[ApiController]
[Route("api/superadmin/feature-flags")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminFeatureFlagsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetFeatureFlagsQuery(), cancellationToken)).ToActionResult();

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] string key, [FromQuery] int? tenantId, CancellationToken cancellationToken) =>
        (await sender.Send(new ResolveFeatureFlagQuery(key, tenantId), cancellationToken)).ToActionResult();

    // Upsert a global or per-tenant flag row (matched on Key + TenantId).
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertFeatureFlagInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new UpsertFeatureFlagCommand(input, Actor()), cancellationToken)).ToActionResult();

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id, [FromBody] ToggleFeatureFlagInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new ToggleFeatureFlagCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteFeatureFlagCommand(id, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
