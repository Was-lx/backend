using WaslX.Application.Abstractions.Channels;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Channels.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Channels;

// ── Queries ──
public record GetChannelsQuery(int? TenantId) : IQuery<IReadOnlyList<ChannelResponse>>;
public class GetChannelsQueryHandler(IChannelService svc) : IQueryHandler<GetChannelsQuery, IReadOnlyList<ChannelResponse>>
{
    public Task<Result<IReadOnlyList<ChannelResponse>>> Handle(GetChannelsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(request.TenantId, cancellationToken);
}

public record GetChannelByIdQuery(int? TenantId, int Id) : IQuery<ChannelResponse>;
public class GetChannelByIdQueryHandler(IChannelService svc) : IQueryHandler<GetChannelByIdQuery, ChannelResponse>
{
    public Task<Result<ChannelResponse>> Handle(GetChannelByIdQuery request, CancellationToken cancellationToken) =>
        svc.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
}

// ── Commands ──
public record CreateChannelCommand(int? TenantId, UpsertChannelRequest Request) : ICommand<ChannelResponse>;
public class CreateChannelCommandHandler(IChannelService svc) : ICommandHandler<CreateChannelCommand, ChannelResponse>
{
    public Task<Result<ChannelResponse>> Handle(CreateChannelCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.TenantId, request.Request, cancellationToken);
}

public record UpdateChannelCommand(int? TenantId, int Id, UpsertChannelRequest Request) : ICommand<ChannelResponse>;
public class UpdateChannelCommandHandler(IChannelService svc) : ICommandHandler<UpdateChannelCommand, ChannelResponse>
{
    public Task<Result<ChannelResponse>> Handle(UpdateChannelCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAsync(request.TenantId, request.Id, request.Request, cancellationToken);
}

public record DeleteChannelCommand(int? TenantId, int Id) : ICommand;
public class DeleteChannelCommandHandler(IChannelService svc) : ICommandHandler<DeleteChannelCommand>
{
    public Task<Result> Handle(DeleteChannelCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAsync(request.TenantId, request.Id, cancellationToken);
}
