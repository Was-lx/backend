using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Reporting;

namespace WaslX.Api.Controllers;

/// <summary>
/// Reporting &amp; analytics dashboards (US-5.4) and CSV/PDF export (US-5.5). Read-only.
/// Admin/Manager see workspace-wide figures; an Agent sees only their own metrics.
/// All endpoints accept optional <c>from</c>/<c>to</c> dates (default last 30 days) and an
/// optional <c>teamId</c> (Group) filter.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Manager,Agent")]
[Route("api/reports")]
public class ReportsController(ISender sender) : ControllerBase
{
    private static (DateTime from, DateTime to) Range(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1); // inclusive end-of-day
        var start = (from ?? end.Date.AddDays(-29)).Date;
        return (start, end);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? teamId, CancellationToken cancellationToken)
    {
        var (f, t) = Range(from, to);
        return (await sender.Send(new GetOverviewQuery(User.GetTenantId(), User.GetRole(), User.GetDomainUserId(), teamId, f, t), cancellationToken)).ToActionResult();
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAgents([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? teamId, CancellationToken cancellationToken)
    {
        var (f, t) = Range(from, to);
        return (await sender.Send(new GetAgentPerformanceQuery(User.GetTenantId(), User.GetRole(), User.GetDomainUserId(), teamId, f, t), cancellationToken)).ToActionResult();
    }

    [HttpGet("response-times")]
    public async Task<IActionResult> GetResponseTimes([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? teamId, CancellationToken cancellationToken)
    {
        var (f, t) = Range(from, to);
        return (await sender.Send(new GetResponseTimesQuery(User.GetTenantId(), User.GetRole(), User.GetDomainUserId(), teamId, f, t), cancellationToken)).ToActionResult();
    }

    [HttpGet("volume")]
    public async Task<IActionResult> GetVolume([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? teamId, CancellationToken cancellationToken)
    {
        var (f, t) = Range(from, to);
        return (await sender.Send(new GetConversationVolumeQuery(User.GetTenantId(), User.GetRole(), User.GetDomainUserId(), teamId, f, t), cancellationToken)).ToActionResult();
    }

    [HttpGet("routing")]
    public async Task<IActionResult> GetRouting([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? teamId, CancellationToken cancellationToken)
    {
        var (f, t) = Range(from, to);
        return (await sender.Send(new GetRoutingStatsQuery(User.GetTenantId(), User.GetRole(), User.GetDomainUserId(), teamId, f, t), cancellationToken)).ToActionResult();
    }

    [HttpGet("{reportKey}/export")]
    public async Task<IActionResult> Export(string reportKey, [FromQuery] string format = "csv", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int? teamId = null, CancellationToken cancellationToken = default)
    {
        var (f, t) = Range(from, to);
        var result = await sender.Send(new ExportReportQuery(User.GetTenantId(), User.GetRole(), User.GetDomainUserId(), teamId, reportKey, format, f, t), cancellationToken);
        if (result.IsFailure)
            return result.ToProblem();

        var file = result.Value;
        return File(file.Bytes, file.ContentType, file.FileName);
    }
}
