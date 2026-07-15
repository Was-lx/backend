using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Assignments;
using WaslX.Application.Features.Assignments.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>Manual assignment / reassignment, per-conversation history and the Unassigned queue. Admin/Manager only.</summary>
[ApiController]
[Route("api/conversations")]
[Authorize(Roles = "Admin,Manager")]
public class AssignmentController(ISender sender) : ControllerBase
{
    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new AssignConversationCommand(User.GetTenantId(), id, request.TargetUserId, User.GetUserId() ?? string.Empty, request.Reason), cancellationToken)).ToActionResult();

    [HttpPost("{id:int}/reassign")]
    public async Task<IActionResult> Reassign(int id, [FromBody] AssignRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new ReassignConversationCommand(User.GetTenantId(), id, request.TargetUserId, User.GetUserId() ?? string.Empty, request.Reason), cancellationToken)).ToActionResult();

    [HttpGet("{id:int}/assignments")]
    public async Task<IActionResult> History(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetAssignmentHistoryQuery(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpGet("unassigned")]
    public async Task<IActionResult> Unassigned(CancellationToken cancellationToken) =>
        (await sender.Send(new GetUnassignedConversationsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();
}
