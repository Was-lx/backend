using WaslX.Application.Abstractions.AgentAccess;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.AgentAccess.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.AgentAccess;

// ── Queries ──
public record GetAgentAccessQuery(int? TenantId, string AppUserId) : IQuery<AgentAccessResponse>;
public class GetAgentAccessQueryHandler(IAgentAccessService svc) : IQueryHandler<GetAgentAccessQuery, AgentAccessResponse>
{
    public Task<Result<AgentAccessResponse>> Handle(GetAgentAccessQuery request, CancellationToken cancellationToken) =>
        svc.GetAccessAsync(request.TenantId, request.AppUserId, cancellationToken);
}

// ── Commands ──
public record SetAgentAccessCommand(int? TenantId, string AppUserId, SetAgentAccessRequest Request) : ICommand<AgentAccessResponse>;
public class SetAgentAccessCommandHandler(IAgentAccessService svc) : ICommandHandler<SetAgentAccessCommand, AgentAccessResponse>
{
    public Task<Result<AgentAccessResponse>> Handle(SetAgentAccessCommand request, CancellationToken cancellationToken) =>
        svc.SetAccessAsync(request.TenantId, request.AppUserId, request.Request, cancellationToken);
}
