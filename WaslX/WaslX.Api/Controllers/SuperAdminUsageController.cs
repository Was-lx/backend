using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;

namespace WaslX.Api.Controllers;

/// <summary>
/// Platform-wide usage monitoring (US-6.4). Cross-tenant, read-only — guarded by the SuperAdmin role.
/// </summary>
[ApiController]
[Route("api/superadmin/usage")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminUsageController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsage([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken) =>
        (await sender.Send(new GetPlatformUsageQuery(from, to), cancellationToken)).ToActionResult();
}
