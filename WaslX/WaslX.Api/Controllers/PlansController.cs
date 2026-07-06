using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Plans;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/plans")]
[AllowAnonymous]
public class PlansController(ISender sender) : ControllerBase
{
    /// <summary>Public pricing — active, public plans for the landing page & upgrade screen.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPublic(CancellationToken cancellationToken) =>
        (await sender.Send(new GetPublicPlansQuery(), cancellationToken)).ToActionResult();
}
