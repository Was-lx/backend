using WaslX.Application.Features.Audit.Dtos;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;

namespace WaslX.Application.Abstractions.Audit;

/// <summary>
/// Immutable, tenant-scoped audit trail (FR-AUDIT / US-5.7). <see cref="WriteAsync"/> appends an
/// entry and is called best-effort by feature services after their primary action has been
/// persisted — it must never break the action that triggered it. <see cref="GetAsync"/> is the
/// read-only, filtered + paged query surface exposed to Admin/Manager.
/// </summary>
public interface IAuditService
{
    Task WriteAsync(int tenantId, int? actorUserId, AuditAction action, string entityType, string entityId, string? details, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<AuditLogResponse>>> GetAsync(int? tenantId, AuditQuery query, CancellationToken cancellationToken = default);
}
