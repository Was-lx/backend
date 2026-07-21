using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.6 · Platform credentials / secrets (masked reads, encrypted at rest) ──
public record GetPlatformCredentialsQuery() : IQuery<IReadOnlyList<PlatformCredentialResponse>>;
public class GetPlatformCredentialsQueryHandler(IPlatformCredentialService svc) : IQueryHandler<GetPlatformCredentialsQuery, IReadOnlyList<PlatformCredentialResponse>>
{
    public Task<Result<IReadOnlyList<PlatformCredentialResponse>>> Handle(GetPlatformCredentialsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(cancellationToken);
}

public record CreatePlatformCredentialCommand(CreatePlatformCredentialInput Input, PlatformActor Actor) : ICommand<PlatformCredentialResponse>;
public class CreatePlatformCredentialCommandHandler(IPlatformCredentialService svc) : ICommandHandler<CreatePlatformCredentialCommand, PlatformCredentialResponse>
{
    public Task<Result<PlatformCredentialResponse>> Handle(CreatePlatformCredentialCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.Input, request.Actor, cancellationToken);
}

public record UpdatePlatformCredentialCommand(int Id, UpdatePlatformCredentialInput Input, PlatformActor Actor) : ICommand<PlatformCredentialResponse>;
public class UpdatePlatformCredentialCommandHandler(IPlatformCredentialService svc) : ICommandHandler<UpdatePlatformCredentialCommand, PlatformCredentialResponse>
{
    public Task<Result<PlatformCredentialResponse>> Handle(UpdatePlatformCredentialCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAsync(request.Id, request.Input, request.Actor, cancellationToken);
}

public record RotatePlatformCredentialCommand(int Id, RotatePlatformCredentialInput Input, PlatformActor Actor) : ICommand<PlatformCredentialResponse>;
public class RotatePlatformCredentialCommandHandler(IPlatformCredentialService svc) : ICommandHandler<RotatePlatformCredentialCommand, PlatformCredentialResponse>
{
    public Task<Result<PlatformCredentialResponse>> Handle(RotatePlatformCredentialCommand request, CancellationToken cancellationToken) =>
        svc.RotateAsync(request.Id, request.Input, request.Actor, cancellationToken);
}

public record DeletePlatformCredentialCommand(int Id, PlatformActor Actor) : ICommand;
public class DeletePlatformCredentialCommandHandler(IPlatformCredentialService svc) : ICommandHandler<DeletePlatformCredentialCommand>
{
    public Task<Result> Handle(DeletePlatformCredentialCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAsync(request.Id, request.Actor, cancellationToken);
}
