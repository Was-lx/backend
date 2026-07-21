namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>
/// Cross-tenant platform usage snapshot (US-6.4). Aggregates conversations, messages and agents across
/// EVERY tenant (no tenant filter — reached only through the SuperAdmin console). The daily series is
/// bounded by the optional query range (defaults to the last 30 days).
/// </summary>
public record PlatformUsageResponse(
    int ActiveTenants,
    int TotalTenants,
    long TotalConversations,
    long TotalMessages,
    int ActiveAgents,
    IReadOnlyList<TenantUsageRow> PerTenant,
    IReadOnlyList<UsageDailyPoint> Daily);

/// <summary>Per-tenant usage breakdown row.</summary>
public record TenantUsageRow(
    int TenantId,
    string TenantName,
    string Status,
    long Conversations,
    long Messages,
    int Agents);

/// <summary>A single day in the platform-wide usage time series.</summary>
public record UsageDailyPoint(DateTime Date, long Conversations, long Messages);

/// <summary>Optional date window for the daily usage series (inclusive). Defaults to the last 30 days.</summary>
public record PlatformUsageQuery(DateTime? From, DateTime? To);
