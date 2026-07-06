using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Tenants;
using WaslX.Application.Features.Tenants.Dtos;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/workspace")]
[Authorize(Roles = "Admin")]
public class WorkspaceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new GetMyTenantQuery(tenantId), cancellationToken)).ToActionResult();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateTenantProfileInput input, CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new UpdateMyTenantCommand(tenantId, input), cancellationToken)).ToActionResult();
    }

    [HttpPut("onboarding")]
    public async Task<IActionResult> UpdateOnboarding([FromBody] OnboardingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new UpdateOnboardingCommand(tenantId, request.Step, request.Completed), cancellationToken)).ToActionResult();
    }

    private IActionResult NoTenant() => BadRequest(new { code = "Tenant.NoContext", description = "This account is not attached to a workspace" });
}
