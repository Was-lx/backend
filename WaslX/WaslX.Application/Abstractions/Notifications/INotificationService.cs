using WaslX.Application.Features.Notifications.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Notifications;

/// <summary>
/// Per-user, tenant-scoped in-app notifications. Writes persist a row and push it in real-time
/// over SignalR; reads/mutations are always scoped by the caller's tenant AND domain user id.
/// </summary>
public interface INotificationService
{
    /// <summary>Persists a notification for a user and pushes it in real-time. Returns the new id.</summary>
    Task<Result<int>> CreateAsync(int tenantId, int userId, string type, string title, string body, string? entityType, int? entityId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<NotificationResponse>>> GetForUserAsync(int? tenantId, int? domainUserId, bool unreadOnly, int take, CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(int? tenantId, int? domainUserId, int id, CancellationToken cancellationToken = default);

    Task<Result> MarkAllReadAsync(int? tenantId, int? domainUserId, CancellationToken cancellationToken = default);

    Task<Result<int>> GetUnreadCountAsync(int? tenantId, int? domainUserId, CancellationToken cancellationToken = default);
}
