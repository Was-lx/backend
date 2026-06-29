using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Roles.GetRoles;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken) =>
        (await sender.Send(new GetRolesQuery(), cancellationToken)).ToActionResult();
}
