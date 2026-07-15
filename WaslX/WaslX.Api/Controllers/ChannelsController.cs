using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Channels;
using WaslX.Application.Features.Channels.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>Channel management — grouping WhatsApp numbers for agent access. Admin/Manager only.</summary>
[ApiController]
[Route("api/channels")]
[Authorize(Roles = "Admin,Manager")]
public class ChannelsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetChannels(CancellationToken cancellationToken) =>
        (await sender.Send(new GetChannelsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetChannel(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetChannelByIdQuery(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> CreateChannel([FromBody] UpsertChannelRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateChannelCommand(User.GetTenantId(), request), cancellationToken)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateChannel(int id, [FromBody] UpsertChannelRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateChannelCommand(User.GetTenantId(), id, request), cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteChannel(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteChannelCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();
}
