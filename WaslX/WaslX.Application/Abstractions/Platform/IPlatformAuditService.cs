using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Global, immutable platform-level audit trail (Sprint 6 — Platform Owner / Super Admin console).
/// Unlike the tenant-scoped audit service, this is NOT filtered by a tenant: it records the actions of
/// a platform actor (SuperAdmin) across all tenants. <see cref="WriteAsync"/> appends an entry
/// best-effort after a platform action has been performed; <see cref="GetAsync"/> is the read-only,
/// filtered + paged query surface exposed to the Platform Owner.
/// </summary>
public interface IPlatformAuditService
{
    Task WriteAsync(string actorId, string actorEmail, string action, string entityType, string entityId, int? tenantId, string? details, string? ip, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<PlatformAuditLogResponse>>> GetAsync(PlatformAuditQuery query, CancellationToken cancellationToken = default);
}
