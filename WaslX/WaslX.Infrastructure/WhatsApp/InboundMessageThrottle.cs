using System.Collections.Concurrent;
using WaslX.Application.Abstractions.WhatsApp;

namespace WaslX.Infrastructure.WhatsApp;

/// <summary>
/// In-memory fixed-window limiter keyed by customer phone number. Singleton by design — the counters
/// must persist across requests on this instance for the window to mean anything. A generous limit
/// (real conversations rarely exceed a handful of messages a minute) so it only ever engages against
/// a scripted flood, not a chatty customer.
/// </summary>
internal sealed class InboundMessageThrottle : IInboundMessageThrottle
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private const int MaxPerWindow = 10;

    private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _counters = new();

    public bool TryAcquire(string phoneNumber)
    {
        var now = DateTime.UtcNow;
        var entry = _counters.AddOrUpdate(
            phoneNumber,
            _ => (1, now),
            (_, existing) => now - existing.WindowStart > Window ? (1, now) : (existing.Count + 1, existing.WindowStart));

        return entry.Count <= MaxPerWindow;
    }
}
