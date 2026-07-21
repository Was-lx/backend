using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Audit;
using WaslX.Application.Features.Audit.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Tenant-scoped immutable audit trail (FR-AUDIT / US-5.7). Every query is filtered by
/// <c>TenantId</c> — a missing filter would be a cross-tenant leak. Entries are only ever
/// appended (<see cref="WriteAsync"/>); there is no update or delete surface.
/// </summary>
internal sealed class AuditService(ApplicationDbContext db) : IAuditService
{
    // Guard rails for paging so a client can neither ask for page 0 nor pull an unbounded set.
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public async Task WriteAsync(int tenantId, int? actorUserId, AuditAction action, string entityType, string entityId, string? details, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType ?? string.Empty,
            EntityId = int.TryParse(entityId, out var eid) ? eid : 0,
            Details = details ?? string.Empty
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<PagedResult<AuditLogResponse>>> GetAsync(int? tenantId, AuditQuery query, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<PagedResult<AuditLogResponse>>(AppErrors.NoTenantContext);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : query.PageSize;

        var q = db.AuditLogs.AsNoTracking().Where(a => a.TenantId == tid);

        if (query.ActorUserId is { } actor)
            q = q.Where(a => a.ActorUserId == actor);
        if (query.Action is { } action)
            q = q.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            q = q.Where(a => a.EntityType == entityType);
        }
        if (query.From is { } from)
            q = q.Where(a => a.CreatedAt >= from);
        if (query.To is { } to)
            q = q.Where(a => a.CreatedAt <= to);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(a => a.Details.Contains(search) || a.EntityType.Contains(search));
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse(
                a.Id,
                a.ActorUserId,
                a.ActorUser != null ? a.ActorUser.Name : null,
                a.Action.ToString(),
                a.EntityType,
                a.EntityId.ToString(),
                a.Details == "" ? null : a.Details,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<AuditLogResponse>(items, total));
    }
}
