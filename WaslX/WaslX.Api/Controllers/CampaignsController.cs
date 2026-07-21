using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Campaigns;
using WaslX.Application.Features.Campaigns.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Outbound WhatsApp broadcast campaigns (FR-CMP): create with an approved template + an audience,
/// preview the audience, schedule/launch the Hangfire send engine, and pause/resume/cancel with
/// per-recipient delivery tracking. Admin/Manager only — Agents cannot run campaigns.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class CampaignsController(ISender sender) : ControllerBase
{
    [HttpGet("api/campaigns")]
    public async Task<IActionResult> GetCampaigns(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCampaignsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpGet("api/campaigns/{id:int}")]
    public async Task<IActionResult> GetCampaign(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetCampaignByIdQuery(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateCampaignCommand(User.GetTenantId(), User.GetDomainUserId(), request), cancellationToken)).ToActionResult();

    [HttpPut("api/campaigns/{id:int}")]
    public async Task<IActionResult> UpdateCampaign(int id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateCampaignCommand(User.GetTenantId(), id, request), cancellationToken)).ToActionResult();

    [HttpDelete("api/campaigns/{id:int}")]
    public async Task<IActionResult> DeleteCampaign(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteCampaignCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/campaigns/preview-audience")]
    public async Task<IActionResult> PreviewAudience([FromBody] AudiencePreviewRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new PreviewAudienceCommand(User.GetTenantId(), request), cancellationToken)).ToActionResult();

    [HttpGet("api/campaigns/audience-contacts")]
    public async Task<IActionResult> GetAudienceContacts([FromQuery] int? tagId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetAudienceContactsQuery(User.GetTenantId(), tagId, dateFrom, dateTo), cancellationToken)).ToActionResult();

    [HttpPost("api/campaigns/{id:int}/launch")]
    public async Task<IActionResult> LaunchCampaign(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new LaunchCampaignCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/campaigns/{id:int}/pause")]
    public async Task<IActionResult> PauseCampaign(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new PauseCampaignCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/campaigns/{id:int}/resume")]
    public async Task<IActionResult> ResumeCampaign(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new ResumeCampaignCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpPost("api/campaigns/{id:int}/cancel")]
    public async Task<IActionResult> CancelCampaign(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new CancelCampaignCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();

    [HttpGet("api/campaigns/{id:int}/recipients")]
    public async Task<IActionResult> GetRecipients(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetCampaignRecipientsQuery(User.GetTenantId(), id), cancellationToken)).ToActionResult();
}
