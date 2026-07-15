using System.Threading;
using System.Threading.Tasks;

namespace WaslX.Application.Abstractions.Maintenance;

/// <summary>
/// Recurring background maintenance for the distribution engine. Registered as Hangfire recurring jobs
/// (see WaslX.Api/Program.cs). Each method is best-effort and self-contained: it logs its own outcome and
/// never throws out to Hangfire so a single bad tenant/agent can't abort the whole run.
/// </summary>
public interface IMaintenanceJobs
{
    /// <summary>
    /// Auto-resolve stale conversations: for every tenant with AutoResolveEnabled, marks Resolved any
    /// non-Resolved conversation whose LastMessageAt is older than the tenant's AutoResolveHours window.
    /// </summary>
    Task AutoResolveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reassign work off offline agents: finds domain users considered offline (IsOnline == false and no
    /// recent heartbeat) and re-runs distribution for each via <c>IDistributionService.ReassignOpenFromAsync</c>.
    /// </summary>
    Task ReassignOfflineAgentsAsync(CancellationToken cancellationToken = default);
}
