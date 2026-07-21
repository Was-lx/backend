namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>
/// A feature toggle (US-6.7). A row with <see cref="TenantId"/> = null is the GLOBAL default for its
/// <see cref="Key"/>; a row with a tenant id is a per-tenant override. The (Key, TenantId) pair is unique.
/// </summary>
public record FeatureFlagResponse(
    int Id,
    string Key,
    string DisplayName,
    string? Description,
    int? TenantId,
    bool IsEnabled,
    DateTime CreatedAt);

/// <summary>
/// Create-or-update a flag row. <paramref name="TenantId"/> null = the global default row; otherwise a
/// per-tenant override. Matched on (Key, TenantId): an existing row is updated, else a new row is created.
/// </summary>
public record UpsertFeatureFlagInput(
    string Key,
    string DisplayName,
    string? Description,
    int? TenantId,
    bool IsEnabled);

/// <summary>Flip a single flag row's enabled state by id.</summary>
public record ToggleFeatureFlagInput(bool IsEnabled);

/// <summary>
/// The resolved effective value of a flag for a given tenant: a per-tenant row wins over the global row.
/// <see cref="Source"/> is "tenant", "global", or "default" (no row at all → treated as disabled).
/// </summary>
public record ResolvedFeatureFlagResponse(string Key, int? TenantId, bool IsEnabled, string Source);
