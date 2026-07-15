using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Tags;
using WaslX.Application.Features.Tags.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>Tag management and applying/removing tags on conversations.</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class TagsController(ISender sender) : ControllerBase
{
    [HttpGet("api/tags")]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken) =>
        (await sender.Send(new GetTagsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpPost("api/tags")]
    public async Task<IActionResult> CreateTag([FromBody] UpsertTagRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateTagCommand(User.GetTenantId(), request), cancellationToken)).ToActionResult();

    [HttpPut("api/tags/{id:int}")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpsertTagRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateTagCommand(User.GetTenantId(), id, request), cancellationToken)).ToActionResult();

    [HttpDelete("api/tags/{id:int}")]
    public async Task<IActionResult> DeleteTag(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteTagCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/conversations/{conversationId:int}/tags/{tagId:int}")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> ApplyTag(int conversationId, int tagId, CancellationToken cancellationToken) =>
        (await sender.Send(new ApplyTagCommand(User.GetTenantId(), conversationId, tagId), cancellationToken)).ToActionResult();

    [HttpDelete("api/conversations/{conversationId:int}/tags/{tagId:int}")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> RemoveTag(int conversationId, int tagId, CancellationToken cancellationToken) =>
        (await sender.Send(new RemoveTagCommand(User.GetTenantId(), conversationId, tagId), cancellationToken)).ToActionResult();
}
