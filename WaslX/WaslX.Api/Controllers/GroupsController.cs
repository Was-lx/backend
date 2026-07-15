using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Groups;
using WaslX.Application.Features.Groups.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>Groups / teams, their stages and membership. Admin/Manager only.</summary>
[ApiController]
[Route("api/groups")]
[Authorize(Roles = "Admin,Manager")]
public class GroupsController(ISender sender) : ControllerBase
{
    // ── Groups ──
    [HttpGet]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken) =>
        (await sender.Send(new GetGroupsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetGroup(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetGroupByIdQuery(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] UpsertGroupRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateGroupCommand(User.GetTenantId(), request), cancellationToken)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpsertGroupRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateGroupCommand(User.GetTenantId(), id, request), cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteGroupCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    // ── Stages ──
    [HttpPost("{groupId:int}/stages")]
    public async Task<IActionResult> CreateStage(int groupId, [FromBody] UpsertStageRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateStageCommand(User.GetTenantId(), groupId, request), cancellationToken)).ToActionResult();

    [HttpPut("{groupId:int}/stages/{stageId:int}")]
    public async Task<IActionResult> UpdateStage(int groupId, int stageId, [FromBody] UpsertStageRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateStageCommand(User.GetTenantId(), groupId, stageId, request), cancellationToken)).ToActionResult();

    [HttpDelete("{groupId:int}/stages/{stageId:int}")]
    public async Task<IActionResult> DeleteStage(int groupId, int stageId, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteStageCommand(User.GetTenantId(), groupId, stageId), cancellationToken)).ToActionResult();

    [HttpPut("{groupId:int}/stages/reorder")]
    public async Task<IActionResult> ReorderStages(int groupId, [FromBody] ReorderStagesRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new ReorderStagesCommand(User.GetTenantId(), groupId, request), cancellationToken)).ToActionResult();

    // ── Membership ──
    [HttpPost("{groupId:int}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddGroupMemberRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new AddGroupMemberCommand(User.GetTenantId(), groupId, request.UserId), cancellationToken)).ToActionResult();

    [HttpDelete("{groupId:int}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int groupId, string userId, CancellationToken cancellationToken) =>
        (await sender.Send(new RemoveGroupMemberCommand(User.GetTenantId(), groupId, userId), cancellationToken)).ToActionResult();
}
