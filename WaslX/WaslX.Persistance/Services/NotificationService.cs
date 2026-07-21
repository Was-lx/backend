using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Abstractions.Realtime;
using WaslX.Application.Features.Notifications.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class NotificationService(ApplicationDbContext db, IInboxRealtimeNotifier notifier) : INotificationService
{
    public async Task<Result<int>> CreateAsync(int tenantId, int userId, string type, string title, string body, string? entityType, int? entityId, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type ?? string.Empty,
            Title = title ?? string.Empty,
            Body = body ?? string.Empty,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var payload = new NotificationResponse(
            notification.Id, notification.Type, notification.Title, notification.Body,
            notification.EntityType, notification.EntityId, notification.IsRead, notification.CreatedAt);
        await notifier.NotificationCreatedAsync(tenantId, userId, payload, cancellationToken);

        return Result.Success(notification.Id);
    }

    public async Task<Result<IReadOnlyList<NotificationResponse>>> GetForUserAsync(int? tenantId, int? domainUserId, bool unreadOnly, int take, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<NotificationResponse>>(AppErrors.NoTenantContext);
        if (domainUserId is not { } uid)
            return Result.Failure<IReadOnlyList<NotificationResponse>>(AppErrors.UserContextNotResolved);

        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        var query = db.Notifications.AsNoTracking()
            .Where(n => n.TenantId == tid && n.UserId == uid);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .Select(n => new NotificationResponse(
                n.Id, n.Type, n.Title, n.Body, n.EntityType, n.EntityId, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<NotificationResponse>>(items);
    }

    public async Task<Result> MarkReadAsync(int? tenantId, int? domainUserId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);
        if (domainUserId is not { } uid)
            return Result.Failure(AppErrors.UserContextNotResolved);

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tid && n.UserId == uid, cancellationToken);
        if (notification is null)
            return Result.Failure(AppErrors.NotificationNotFound);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> MarkAllReadAsync(int? tenantId, int? domainUserId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);
        if (domainUserId is not { } uid)
            return Result.Failure(AppErrors.UserContextNotResolved);

        var unread = await db.Notifications
            .Where(n => n.TenantId == tid && n.UserId == uid && !n.IsRead)
            .ToListAsync(cancellationToken);
        if (unread.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.UpdatedAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<int>> GetUnreadCountAsync(int? tenantId, int? domainUserId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<int>(AppErrors.NoTenantContext);
        if (domainUserId is not { } uid)
            return Result.Failure<int>(AppErrors.UserContextNotResolved);

        var count = await db.Notifications.AsNoTracking()
            .CountAsync(n => n.TenantId == tid && n.UserId == uid && !n.IsRead, cancellationToken);

        return Result.Success(count);
    }
}
