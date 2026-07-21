using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Abstractions.Screening;
using WaslX.Application.Features.Escalation.Screening;
using WaslX.Domain.Results;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/escalation")]
[Authorize]
public class EscalationController(
    IEscalationModeService modeService,
    IEscalationAssignmentService assignmentService) : ControllerBase
{
    // ── Settings ──

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        if (tenantId is null)
            return Unauthorized();

        return (await modeService.GetSettingsAsync(tenantId.Value, cancellationToken)).ToActionResult();
    }

    [HttpPut("settings")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateEscalationModeRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var userId = User.GetDomainUserId();
        if (tenantId is null || userId is null)
            return Unauthorized();

        return (await modeService.UpdateSettingsAsync(tenantId.Value, userId.Value, request.Mode, cancellationToken)).ToActionResult();
    }

    // ── Recommendation ──

    [HttpGet("~/api/conversations/{conversationId:int}/escalation-recommendation")]
    public async Task<IActionResult> GetRecommendation(int conversationId, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        if (tenantId is null)
            return Unauthorized();

        return (await assignmentService.GetRecommendationAsync(tenantId.Value, conversationId, cancellationToken)).ToActionResult();
    }

    // ── Confirm / Override ──

    [HttpPost("escalations/{escalationId:int}/confirm")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Confirm(
        int escalationId,
        [FromBody] ConfirmEscalationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var userId = User.GetDomainUserId();
        if (tenantId is null || userId is null)
            return Unauthorized();

        return (await assignmentService.ConfirmAsync(tenantId.Value, userId.Value, escalationId, request.AssigneeId, cancellationToken)).ToActionResult();
    }

    [HttpPost("escalations/{escalationId:int}/override")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Override(
        int escalationId,
        [FromBody] OverrideEscalationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var userId = User.GetDomainUserId();
        if (tenantId is null || userId is null)
            return Unauthorized();

        return (await assignmentService.OverrideAsync(tenantId.Value, userId.Value, escalationId, request.AssigneeId, request.Reason, cancellationToken)).ToActionResult();
    }
}
