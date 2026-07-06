using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Tenants;
using WaslX.Application.Features.Tenants.Dtos;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/superadmin/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminTenantsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetTenantsQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SuperAdminCreateTenantInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateTenantCommand(input), cancellationToken)).ToActionResult();

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetTenantStatusRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetTenantStatusCommand(id, request.Status), cancellationToken)).ToActionResult();
}
