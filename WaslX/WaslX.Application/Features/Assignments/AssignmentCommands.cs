using WaslX.Application.Abstractions.Assignments;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Assignments.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Assignments;

// ── Commands ──
public record AssignConversationCommand(int? TenantId, int ConversationId, string TargetUserId, string ByUserId, string? Reason) : ICommand;
public class AssignConversationCommandHandler(IAssignmentService svc) : ICommandHandler<AssignConversationCommand>
{
    public Task<Result> Handle(AssignConversationCommand request, CancellationToken cancellationToken) =>
        svc.AssignAsync(request.TenantId, request.ConversationId, request.TargetUserId, request.ByUserId, request.Reason, cancellationToken);
}

public record ReassignConversationCommand(int? TenantId, int ConversationId, string TargetUserId, string ByUserId, string? Reason) : ICommand;
public class ReassignConversationCommandHandler(IAssignmentService svc) : ICommandHandler<ReassignConversationCommand>
{
    public Task<Result> Handle(ReassignConversationCommand request, CancellationToken cancellationToken) =>
        svc.ReassignAsync(request.TenantId, request.ConversationId, request.TargetUserId, request.ByUserId, request.Reason, cancellationToken);
}

// ── Queries ──
public record GetAssignmentHistoryQuery(int? TenantId, int ConversationId) : IQuery<IReadOnlyList<AssignmentHistoryItem>>;
public class GetAssignmentHistoryQueryHandler(IAssignmentService svc) : IQueryHandler<GetAssignmentHistoryQuery, IReadOnlyList<AssignmentHistoryItem>>
{
    public Task<Result<IReadOnlyList<AssignmentHistoryItem>>> Handle(GetAssignmentHistoryQuery request, CancellationToken cancellationToken) =>
        svc.GetHistoryAsync(request.TenantId, request.ConversationId, cancellationToken);
}

public record GetUnassignedConversationsQuery(int? TenantId) : IQuery<IReadOnlyList<UnassignedConversationItem>>;
public class GetUnassignedConversationsQueryHandler(IAssignmentService svc) : IQueryHandler<GetUnassignedConversationsQuery, IReadOnlyList<UnassignedConversationItem>>
{
    public Task<Result<IReadOnlyList<UnassignedConversationItem>>> Handle(GetUnassignedConversationsQuery request, CancellationToken cancellationToken) =>
        svc.GetUnassignedAsync(request.TenantId, cancellationToken);
}
