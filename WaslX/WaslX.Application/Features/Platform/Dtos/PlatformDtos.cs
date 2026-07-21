namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>A single immutable platform-level audit-trail entry (global, cross-tenant).</summary>
public record PlatformAuditLogResponse(
    int Id,
    string ActorPlatformUserId,
    string ActorEmail,
    string Action,
    string EntityType,
    string EntityId,
    int? TenantId,
    string? Details,
    string? IpAddress,
    DateTime Timestamp);

/// <summary>Filter + paging criteria for querying the global platform audit log (no tenant filter).</summary>
public record PlatformAuditQuery(
    string? ActorEmail,
    string? Action,
    string? EntityType,
    int? TenantId,
    DateTime? From,
    DateTime? To,
    string? Search,
    int Page,
    int PageSize);

/// <summary>A page of results plus the total count matching the filter (before paging).</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total);

/// <summary>
/// The platform actor performing a mutation, threaded from the controller (the authenticated
/// SuperAdmin) down into a command/service so every platform change can be written to the global
/// audit trail. <see cref="ActorId"/> is the Identity GUID (<c>User.GetUserId()</c>) and
/// <see cref="ActorEmail"/> is <c>User.GetEmail()</c>.
/// </summary>
public record PlatformActor(string ActorId, string ActorEmail, string? Ip);
