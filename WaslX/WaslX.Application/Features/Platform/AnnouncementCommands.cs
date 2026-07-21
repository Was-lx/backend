using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.10b · Platform announcements ──
public record GetAnnouncementsQuery() : IQuery<IReadOnlyList<AnnouncementResponse>>;
public class GetAnnouncementsQueryHandler(IAnnouncementService svc) : IQueryHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementResponse>>
{
    public Task<Result<IReadOnlyList<AnnouncementResponse>>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(cancellationToken);
}

public record CreateAnnouncementCommand(CreateAnnouncementInput Input, PlatformActor Actor) : ICommand<AnnouncementResponse>;
public class CreateAnnouncementCommandHandler(IAnnouncementService svc) : ICommandHandler<CreateAnnouncementCommand, AnnouncementResponse>
{
    public Task<Result<AnnouncementResponse>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.Input, request.Actor, cancellationToken);
}

public record PublishAnnouncementCommand(int Id, PlatformActor Actor) : ICommand<AnnouncementResponse>;
public class PublishAnnouncementCommandHandler(IAnnouncementService svc) : ICommandHandler<PublishAnnouncementCommand, AnnouncementResponse>
{
    public Task<Result<AnnouncementResponse>> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken) =>
        svc.PublishAsync(request.Id, request.Actor, cancellationToken);
}

public record DeleteAnnouncementCommand(int Id, PlatformActor Actor) : ICommand;
public class DeleteAnnouncementCommandHandler(IAnnouncementService svc) : ICommandHandler<DeleteAnnouncementCommand>
{
    public Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken) =>
        svc.DeactivateAsync(request.Id, request.Actor, cancellationToken);
}
