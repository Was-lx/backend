using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Presence;
using WaslX.Application.Features.Agents.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Agents;

// ── Queries ──
public record GetMyAvailabilityQuery(int? TenantId, string? Email) : IQuery<AvailabilityResponse>;
public class GetMyAvailabilityQueryHandler(IPresenceService svc) : IQueryHandler<GetMyAvailabilityQuery, AvailabilityResponse>
{
    public Task<Result<AvailabilityResponse>> Handle(GetMyAvailabilityQuery request, CancellationToken cancellationToken) =>
        svc.GetMyAvailabilityAsync(request.TenantId, request.Email, cancellationToken);
}

// ── Commands ──
public record SetBreakCommand(int? TenantId, string? Email, bool OnBreak) : ICommand<AvailabilityResponse>;
public class SetBreakCommandHandler(IPresenceService svc) : ICommandHandler<SetBreakCommand, AvailabilityResponse>
{
    public Task<Result<AvailabilityResponse>> Handle(SetBreakCommand request, CancellationToken cancellationToken) =>
        svc.SetBreakAsync(request.TenantId, request.Email, request.OnBreak, cancellationToken);
}
