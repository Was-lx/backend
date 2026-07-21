using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Global, immutable platform audit trail (Sprint 6 — Platform Owner console). Deliberately
/// CROSS-TENANT: <see cref="GetAsync"/> applies no tenant filter, because the only caller is the
/// SuperAdmin console guarded by <c>[Authorize(Roles = "SuperAdmin")]</c>. Entries are only ever
/// appended (<see cref="WriteAsync"/>); there is no update or delete surface.
/// </summary>
internal sealed class PlatformAuditService(ApplicationDbContext db) : IPlatformAuditService
{
    // Guard rails for paging so a client can neither ask for page 0 nor pull an unbounded set.
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public async Task WriteAsync(string actorId, string actorEmail, string action, string entityType, string entityId, int? tenantId, string? details, string? ip, CancellationToken cancellationToken = default)
    {
        var log = new PlatformAuditLog
        {
            ActorPlatformUserId = actorId ?? string.Empty,
            ActorEmail = actorEmail ?? string.Empty,
            Action = action ?? string.Empty,
            EntityType = entityType ?? string.Empty,
            EntityId = entityId ?? string.Empty,
            TenantId = tenantId,
            Details = details,
            IpAddress = ip
        };

        db.PlatformAuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<PagedResult<PlatformAuditLogResponse>>> GetAsync(PlatformAuditQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : query.PageSize;

        // No tenant filter — this is a global platform log (guarded by SuperAdmin authorization).
        var q = db.PlatformAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.ActorEmail))
        {
            var actorEmail = query.ActorEmail.Trim();
            q = q.Where(a => a.ActorEmail == actorEmail);
        }
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            q = q.Where(a => a.Action == action);
        }
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            q = q.Where(a => a.EntityType == entityType);
        }
        if (query.TenantId is { } tenantId)
            q = q.Where(a => a.TenantId == tenantId);
        if (query.From is { } from)
            q = q.Where(a => a.CreatedAt >= from);
        if (query.To is { } to)
            q = q.Where(a => a.CreatedAt <= to);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(a => (a.Details != null && a.Details.Contains(search))
                || a.EntityType.Contains(search)
                || a.ActorEmail.Contains(search)
                || a.Action.Contains(search));
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new PlatformAuditLogResponse(
                a.Id,
                a.ActorPlatformUserId,
                a.ActorEmail,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.TenantId,
                a.Details,
                a.IpAddress,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<PlatformAuditLogResponse>(items, total));
    }
}
