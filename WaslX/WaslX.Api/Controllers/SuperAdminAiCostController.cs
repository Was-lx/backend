using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Platform-wide AI cost monitoring + budget-alert management (US-6.5). Cross-tenant — guarded by the
/// SuperAdmin role. The cost read never throws when no AI usage has been recorded yet.
/// </summary>
[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminAiCostController(ISender sender) : ControllerBase
{
    // ── AI cost dashboard ──
    [HttpGet("ai-cost")]
    public async Task<IActionResult> GetCost([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken) =>
        (await sender.Send(new GetAiCostQuery(from, to), cancellationToken)).ToActionResult();

    // ── Budget-alert CRUD ──
    [HttpGet("budget-alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken) =>
        (await sender.Send(new GetBudgetAlertsQuery(), cancellationToken)).ToActionResult();

    [HttpPost("budget-alerts")]
    public async Task<IActionResult> CreateAlert([FromBody] CreateBudgetAlertInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateBudgetAlertCommand(input, Actor()), cancellationToken)).ToActionResult();

    [HttpPut("budget-alerts/{id:int}")]
    public async Task<IActionResult> UpdateAlert(int id, [FromBody] UpdateBudgetAlertInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateBudgetAlertCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    [HttpDelete("budget-alerts/{id:int}")]
    public async Task<IActionResult> DeleteAlert(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteBudgetAlertCommand(id, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
