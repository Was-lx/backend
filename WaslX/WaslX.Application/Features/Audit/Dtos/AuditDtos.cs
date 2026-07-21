using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Features.Audit.Dtos;

/// <summary>A single immutable audit-trail entry (tenant-scoped).</summary>
public record AuditLogResponse(
    int Id,
    int? ActorUserId,
    string? ActorName,
    string Action,
    string EntityType,
    string EntityId,
    string? Details,
    DateTime Timestamp);

/// <summary>Filter + paging criteria for querying the tenant audit log.</summary>
public record AuditQuery(
    int? ActorUserId,
    AuditAction? Action,
    string? EntityType,
    DateTime? From,
    DateTime? To,
    string? Search,
    int Page,
    int PageSize);

/// <summary>A page of results plus the total count matching the filter (before paging).</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total);
