using WaslX.Application.Abstractions.ConversationStages;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.ConversationStages.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.ConversationStages;

// ── Commands ──
public record RouteConversationToGroupCommand(int? TenantId, int ConversationId, int GroupId, int ByUserId, string? ByUserEmail, bool IsPrivileged) : ICommand;
public class RouteConversationToGroupCommandHandler(IConversationStageService svc) : ICommandHandler<RouteConversationToGroupCommand>
{
    public Task<Result> Handle(RouteConversationToGroupCommand request, CancellationToken cancellationToken) =>
        svc.RouteToGroupAsync(request.TenantId, request.ConversationId, request.GroupId, request.ByUserId, request.ByUserEmail, request.IsPrivileged, cancellationToken);
}

public record MoveConversationToStageCommand(int? TenantId, int ConversationId, int StageId, int ByUserId, string? ByUserEmail, bool IsPrivileged) : ICommand;
public class MoveConversationToStageCommandHandler(IConversationStageService svc) : ICommandHandler<MoveConversationToStageCommand>
{
    public Task<Result> Handle(MoveConversationToStageCommand request, CancellationToken cancellationToken) =>
        svc.MoveToStageAsync(request.TenantId, request.ConversationId, request.StageId, request.ByUserId, request.ByUserEmail, request.IsPrivileged, cancellationToken);
}

public record HandoffConversationCommand(int? TenantId, int ConversationId, int TargetGroupId, int ByUserId, string? ByUserEmail, bool IsPrivileged) : ICommand;
public class HandoffConversationCommandHandler(IConversationStageService svc) : ICommandHandler<HandoffConversationCommand>
{
    public Task<Result> Handle(HandoffConversationCommand request, CancellationToken cancellationToken) =>
        svc.HandoffAsync(request.TenantId, request.ConversationId, request.TargetGroupId, request.ByUserId, request.ByUserEmail, request.IsPrivileged, cancellationToken);
}

// ── Queries ──
public record GetGroupBoardQuery(int? TenantId, int GroupId, bool IsPrivileged, int CurrentUserId) : IQuery<IReadOnlyList<StageBoardColumn>>;
public class GetGroupBoardQueryHandler(IConversationStageService svc) : IQueryHandler<GetGroupBoardQuery, IReadOnlyList<StageBoardColumn>>
{
    public Task<Result<IReadOnlyList<StageBoardColumn>>> Handle(GetGroupBoardQuery request, CancellationToken cancellationToken) =>
        svc.GetBoardAsync(request.TenantId, request.GroupId, request.IsPrivileged, request.CurrentUserId, cancellationToken);
}
