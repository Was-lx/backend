namespace WaslX.Domain.SharedEnums;

/// <summary>
/// How new inbound conversations on a WhatsApp number are auto-distributed to agents.
/// Configured per WhatsApp number (set in the connect wizard's step 1).
/// </summary>
public enum DistributionMode
{
    /// <summary>No auto-distribution. New conversations stay Unassigned; the tenant owner distributes them manually.</summary>
    ByAdmin = 0,

    /// <summary>Auto-assign to the least-loaded eligible agent (online, not on break, in this number's distribution list).</summary>
    RoundRobin = 1,

    /// <summary>Auto-assign to the least-loaded agent whose shift is currently active for this number. Ignores login/break; driven by the shift schedule.</summary>
    RoundRobinByWorkingHours = 2
}
