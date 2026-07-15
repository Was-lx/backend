using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.ConversationStages;

namespace WaslX.Api.Controllers;

/// <summary>
/// The stage pipeline board for a group / team. Open to any workspace role (unlike group administration):
/// agents see only their own conversations on the board, managers/admins see the whole team.
/// </summary>
[ApiController]
[Route("api/groups")]
[Authorize]
public class StageBoardController(ISender sender) : ControllerBase
{
    [HttpGet("{groupId:int}/board")]
    public async Task<IActionResult> GetBoard(int groupId, CancellationToken cancellationToken)
    {
        var query = new GetGroupBoardQuery(User.GetTenantId(), groupId, IsPrivileged(), CurrentUserId());
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    private int CurrentUserId() => User.GetDomainUserId() ?? 0;

    private bool IsPrivileged() => User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("SuperAdmin");
}
