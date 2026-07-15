using WaslX.Application.Abstractions.Groups;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Groups.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Groups;

// ── Queries ──
public record GetGroupsQuery(int? TenantId) : IQuery<IReadOnlyList<GroupResponse>>;
public class GetGroupsQueryHandler(IGroupService svc) : IQueryHandler<GetGroupsQuery, IReadOnlyList<GroupResponse>>
{
    public Task<Result<IReadOnlyList<GroupResponse>>> Handle(GetGroupsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(request.TenantId, cancellationToken);
}

public record GetGroupByIdQuery(int? TenantId, int Id) : IQuery<GroupResponse>;
public class GetGroupByIdQueryHandler(IGroupService svc) : IQueryHandler<GetGroupByIdQuery, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken) =>
        svc.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
}

// ── Group commands ──
public record CreateGroupCommand(int? TenantId, UpsertGroupRequest Request) : ICommand<GroupResponse>;
public class CreateGroupCommandHandler(IGroupService svc) : ICommandHandler<CreateGroupCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(CreateGroupCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.TenantId, request.Request, cancellationToken);
}

public record UpdateGroupCommand(int? TenantId, int Id, UpsertGroupRequest Request) : ICommand<GroupResponse>;
public class UpdateGroupCommandHandler(IGroupService svc) : ICommandHandler<UpdateGroupCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(UpdateGroupCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAsync(request.TenantId, request.Id, request.Request, cancellationToken);
}

public record DeleteGroupCommand(int? TenantId, int Id) : ICommand;
public class DeleteGroupCommandHandler(IGroupService svc) : ICommandHandler<DeleteGroupCommand>
{
    public Task<Result> Handle(DeleteGroupCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAsync(request.TenantId, request.Id, cancellationToken);
}

// ── Stage commands ──
public record CreateStageCommand(int? TenantId, int GroupId, UpsertStageRequest Request) : ICommand<GroupResponse>;
public class CreateStageCommandHandler(IGroupService svc) : ICommandHandler<CreateStageCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(CreateStageCommand request, CancellationToken cancellationToken) =>
        svc.CreateStageAsync(request.TenantId, request.GroupId, request.Request, cancellationToken);
}

public record UpdateStageCommand(int? TenantId, int GroupId, int StageId, UpsertStageRequest Request) : ICommand<GroupResponse>;
public class UpdateStageCommandHandler(IGroupService svc) : ICommandHandler<UpdateStageCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(UpdateStageCommand request, CancellationToken cancellationToken) =>
        svc.UpdateStageAsync(request.TenantId, request.GroupId, request.StageId, request.Request, cancellationToken);
}

public record DeleteStageCommand(int? TenantId, int GroupId, int StageId) : ICommand<GroupResponse>;
public class DeleteStageCommandHandler(IGroupService svc) : ICommandHandler<DeleteStageCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(DeleteStageCommand request, CancellationToken cancellationToken) =>
        svc.DeleteStageAsync(request.TenantId, request.GroupId, request.StageId, cancellationToken);
}

public record ReorderStagesCommand(int? TenantId, int GroupId, ReorderStagesRequest Request) : ICommand<GroupResponse>;
public class ReorderStagesCommandHandler(IGroupService svc) : ICommandHandler<ReorderStagesCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(ReorderStagesCommand request, CancellationToken cancellationToken) =>
        svc.ReorderStagesAsync(request.TenantId, request.GroupId, request.Request, cancellationToken);
}

// ── Membership commands ──
public record AddGroupMemberCommand(int? TenantId, int GroupId, string UserId) : ICommand<GroupResponse>;
public class AddGroupMemberCommandHandler(IGroupService svc) : ICommandHandler<AddGroupMemberCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(AddGroupMemberCommand request, CancellationToken cancellationToken) =>
        svc.AddMemberAsync(request.TenantId, request.GroupId, request.UserId, cancellationToken);
}

public record RemoveGroupMemberCommand(int? TenantId, int GroupId, string UserId) : ICommand<GroupResponse>;
public class RemoveGroupMemberCommandHandler(IGroupService svc) : ICommandHandler<RemoveGroupMemberCommand, GroupResponse>
{
    public Task<Result<GroupResponse>> Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken) =>
        svc.RemoveMemberAsync(request.TenantId, request.GroupId, request.UserId, cancellationToken);
}
