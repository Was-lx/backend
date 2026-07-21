using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── Queries ──
public record GetSuperAdminsQuery() : IQuery<IReadOnlyList<SuperAdminUserResponse>>;
public class GetSuperAdminsQueryHandler(ISuperAdminUserService svc) : IQueryHandler<GetSuperAdminsQuery, IReadOnlyList<SuperAdminUserResponse>>
{
    public Task<Result<IReadOnlyList<SuperAdminUserResponse>>> Handle(GetSuperAdminsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(cancellationToken);
}

// ── Commands ──
public record CreateSuperAdminCommand(CreateSuperAdminInput Input, PlatformActor Actor) : ICommand<SuperAdminUserResponse>;
public class CreateSuperAdminCommandHandler(ISuperAdminUserService svc) : ICommandHandler<CreateSuperAdminCommand, SuperAdminUserResponse>
{
    public Task<Result<SuperAdminUserResponse>> Handle(CreateSuperAdminCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.Input, request.Actor, cancellationToken);
}

public record SetSuperAdminStatusCommand(string UserId, bool IsDisabled, PlatformActor Actor) : ICommand;
public class SetSuperAdminStatusCommandHandler(ISuperAdminUserService svc) : ICommandHandler<SetSuperAdminStatusCommand>
{
    public Task<Result> Handle(SetSuperAdminStatusCommand request, CancellationToken cancellationToken) =>
        svc.SetStatusAsync(request.UserId, request.IsDisabled, request.Actor, cancellationToken);
}
