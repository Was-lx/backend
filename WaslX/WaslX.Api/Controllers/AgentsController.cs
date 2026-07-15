using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Agents;
using WaslX.Application.Features.Agents.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>The calling agent's own presence &amp; availability (online state + break toggle).</summary>
[ApiController]
[Route("api/agents")]
[Authorize]
public class AgentsController(ISender sender) : ControllerBase
{
    [HttpGet("me/availability")]
    public async Task<IActionResult> GetMyAvailability(CancellationToken cancellationToken) =>
        (await sender.Send(new GetMyAvailabilityQuery(User.GetTenantId(), User.GetEmail()), cancellationToken)).ToActionResult();

    [HttpPatch("me/break")]
    public async Task<IActionResult> SetBreak([FromBody] SetBreakRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetBreakCommand(User.GetTenantId(), User.GetEmail(), request.OnBreak), cancellationToken)).ToActionResult();
}
