using System.Threading;
using System.Threading.Tasks;

namespace WaslX.Application.Abstractions.Distribution;

/// <summary>
/// Auto-distribution engine: routes a new/unassigned conversation to the least-loaded eligible agent
/// according to the owning WhatsApp number's <see cref="WaslX.Domain.SharedEnums.DistributionMode"/>.
/// Load = count of the agent's non-Resolved conversations; ties break on oldest last assignment, then
/// lowest UserId. All queries are tenant-scoped through the conversation's owning number.
/// </summary>
public interface IDistributionService
{
    /// <summary>
    /// Applies the number's distribution mode to the conversation and, if an eligible agent is found,
    /// assigns it (mutates the conversation, writes an Assignment row, notifies the inbox, saves).
    /// Returns the assigned domain UserId, or null when the conversation is left unassigned.
    /// </summary>
    Task<int?> AutoAssignAsync(int conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Offline-reassign job entry point: for every OPEN (Status != Resolved) conversation currently
    /// owned by <paramref name="domainUserId"/> on a number whose mode is RoundRobin with
    /// ReassignOnOffline enabled, clears the assignment and re-runs <see cref="AutoAssignAsync"/> so a
    /// different eligible agent picks it up. Numbers without ReassignOnOffline are never touched.
    /// </summary>
    Task ReassignOpenFromAsync(int domainUserId, CancellationToken cancellationToken = default);
}
