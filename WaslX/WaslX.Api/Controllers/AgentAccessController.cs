using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.AgentAccess;
using WaslX.Application.Features.AgentAccess.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>An agent's access &amp; distribution assignment — channels, distribution numbers, groups, shifts. Admin/Manager only.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin,Manager")]
public class AgentAccessController(ISender sender) : ControllerBase
{
    [HttpGet("{appUserId}/access")]
    public async Task<IActionResult> GetAccess(string appUserId, CancellationToken cancellationToken) =>
        (await sender.Send(new GetAgentAccessQuery(User.GetTenantId(), appUserId), cancellationToken)).ToActionResult();

    [HttpPut("{appUserId}/access")]
    public async Task<IActionResult> SetAccess(string appUserId, [FromBody] SetAgentAccessRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetAgentAccessCommand(User.GetTenantId(), appUserId, request), cancellationToken)).ToActionResult();
}
