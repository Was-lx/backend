using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.7 · Global platform policy (PlatformSetting keys) ──
public record GetPlatformPolicyQuery() : IQuery<PlatformPolicyResponse>;
public class GetPlatformPolicyQueryHandler(IPlatformPolicyService svc) : IQueryHandler<GetPlatformPolicyQuery, PlatformPolicyResponse>
{
    public Task<Result<PlatformPolicyResponse>> Handle(GetPlatformPolicyQuery request, CancellationToken cancellationToken) =>
        svc.GetAsync(cancellationToken);
}

public record SetPlatformPolicyCommand(SetPlatformPolicyInput Input, PlatformActor Actor) : ICommand<PlatformPolicyResponse>;
public class SetPlatformPolicyCommandHandler(IPlatformPolicyService svc) : ICommandHandler<SetPlatformPolicyCommand, PlatformPolicyResponse>
{
    public Task<Result<PlatformPolicyResponse>> Handle(SetPlatformPolicyCommand request, CancellationToken cancellationToken) =>
        svc.SetAsync(request.Input, request.Actor, cancellationToken);
}
