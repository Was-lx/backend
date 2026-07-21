using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Features.Notifications.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Notifications;

// ── Queries ──
public record GetNotificationsQuery(int? TenantId, int? DomainUserId, bool UnreadOnly, int Take) : IQuery<IReadOnlyList<NotificationResponse>>;
public class GetNotificationsQueryHandler(INotificationService svc) : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public Task<Result<IReadOnlyList<NotificationResponse>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken) =>
        svc.GetForUserAsync(request.TenantId, request.DomainUserId, request.UnreadOnly, request.Take, cancellationToken);
}

public record GetUnreadCountQuery(int? TenantId, int? DomainUserId) : IQuery<int>;
public class GetUnreadCountQueryHandler(INotificationService svc) : IQueryHandler<GetUnreadCountQuery, int>
{
    public Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken) =>
        svc.GetUnreadCountAsync(request.TenantId, request.DomainUserId, cancellationToken);
}

// ── Commands ──
public record MarkNotificationReadCommand(int? TenantId, int? DomainUserId, int Id) : ICommand;
public class MarkNotificationReadCommandHandler(INotificationService svc) : ICommandHandler<MarkNotificationReadCommand>
{
    public Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken) =>
        svc.MarkReadAsync(request.TenantId, request.DomainUserId, request.Id, cancellationToken);
}

public record MarkAllNotificationsReadCommand(int? TenantId, int? DomainUserId) : ICommand;
public class MarkAllNotificationsReadCommandHandler(INotificationService svc) : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken) =>
        svc.MarkAllReadAsync(request.TenantId, request.DomainUserId, cancellationToken);
}
