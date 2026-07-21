using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.8 · Audited impersonation ──
public record StartImpersonationCommand(StartImpersonationInput Input, PlatformActor Actor) : ICommand<StartImpersonationResponse>;
public class StartImpersonationCommandHandler(IImpersonationService svc) : ICommandHandler<StartImpersonationCommand, StartImpersonationResponse>
{
    public Task<Result<StartImpersonationResponse>> Handle(StartImpersonationCommand request, CancellationToken cancellationToken) =>
        svc.StartAsync(request.Input, request.Actor, cancellationToken);
}

public record EndImpersonationCommand(int SessionId, PlatformActor Actor) : ICommand<ImpersonationSessionResponse>;
public class EndImpersonationCommandHandler(IImpersonationService svc) : ICommandHandler<EndImpersonationCommand, ImpersonationSessionResponse>
{
    public Task<Result<ImpersonationSessionResponse>> Handle(EndImpersonationCommand request, CancellationToken cancellationToken) =>
        svc.EndAsync(request.SessionId, request.Actor, cancellationToken);
}

public record GetActiveImpersonationsQuery() : IQuery<IReadOnlyList<ImpersonationSessionResponse>>;
public class GetActiveImpersonationsQueryHandler(IImpersonationService svc) : IQueryHandler<GetActiveImpersonationsQuery, IReadOnlyList<ImpersonationSessionResponse>>
{
    public Task<Result<IReadOnlyList<ImpersonationSessionResponse>>> Handle(GetActiveImpersonationsQuery request, CancellationToken cancellationToken) =>
        svc.GetActiveAsync(cancellationToken);
}
