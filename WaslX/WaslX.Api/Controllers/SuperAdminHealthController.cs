using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// System health monitoring for the Platform Owner (US-6.10a). Runs the registered health checks and
/// returns each component's status + latency as JSON. Guarded by the SuperAdmin role. Never throws —
/// any failure is reported as a Down component rather than a 500.
/// </summary>
[ApiController]
[Route("api/superadmin/health")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminHealthController(HealthCheckService healthChecks) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var report = await healthChecks.CheckHealthAsync(cancellationToken);

            var components = report.Entries
                .Select(e => new HealthComponentResponse(
                    e.Key,
                    ToStatus(e.Value.Status),
                    Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                    e.Value.Description ?? e.Value.Exception?.Message))
                .OrderBy(c => c.Name)
                .ToList();

            return Ok(new SystemHealthResponse(ToStatus(report.Status), DateTime.UtcNow, components));
        }
        catch (Exception ex)
        {
            // Health reporting must never surface as a 500 — degrade gracefully to a Down snapshot.
            var down = new List<HealthComponentResponse>
            {
                new("health", "Down", null, ex.Message)
            };
            return Ok(new SystemHealthResponse("Down", DateTime.UtcNow, down));
        }
    }

    private static string ToStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "Healthy",
        HealthStatus.Degraded => "Degraded",
        _ => "Down"
    };
}
