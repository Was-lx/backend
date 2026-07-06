using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Permissions;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsController(ISender sender) : ControllerBase
{
    /// <summary>The tenant's editable Roles × Permissions matrix.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMatrix(CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new GetPermissionMatrixQuery(tenantId), cancellationToken)).ToActionResult();
    }

    /// <summary>Apply toggle changes. Locked cells and the Admin column are ignored server-side.</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePermissionsRequest request, CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new UpdatePermissionMatrixCommand(tenantId, request.Changes), cancellationToken)).ToActionResult();
    }

    private IActionResult NoTenant() => BadRequest(new { code = "Tenant.NoContext", description = "This account is not attached to a workspace" });
}
