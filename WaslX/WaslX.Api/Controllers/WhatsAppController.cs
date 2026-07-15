using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Features.WhatsApp.ConnectAccount;
using WaslX.Application.Features.WhatsApp.Disconnect;
using WaslX.Application.Features.WhatsApp.GetAccount;
using WaslX.Application.Features.WhatsApp.GetAccounts;
using WaslX.Application.Features.WhatsApp.SendTemplateMessage;
using WaslX.Application.Features.WhatsApp.SendTextMessage;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhatsAppController(ISender sender, IMediaStorageService mediaStorage) : ControllerBase
{
    /// <summary>Completes the Facebook Login for Business flow and connects the tenant's WhatsApp number.</summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectWhatsAppRequest request, CancellationToken cancellationToken)
    {
        var command = new ConnectWhatsAppAccountCommand(
            User.GetTenantId(),
            request.AuthorizationCode,
            request.WabaId,
            request.RedirectUri,
            request.PlatformName,
            request.DistributionMode,
            request.DistributeToOffline,
            request.ReassignOnOffline,
            request.StartingGroupId);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Returns the tenant's WhatsApp connection status (never the access token).</summary>
    [HttpGet("account")]
    public async Task<IActionResult> GetAccount(CancellationToken cancellationToken) =>
        (await sender.Send(new GetWhatsAppAccountQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    /// <summary>Lists ALL of the tenant's WhatsApp numbers as a light list (never the access token).</summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken) =>
        (await sender.Send(new GetWhatsAppAccountsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken) =>
        (await sender.Send(new DisconnectWhatsAppAccountCommand(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpPost("messages/text")]
    public async Task<IActionResult> SendText([FromBody] SendWhatsAppTextRequest request, CancellationToken cancellationToken)
    {
        var command = new SendTextMessageCommand(User.GetTenantId(), request.ToPhone, request.Text);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPost("messages/template")]
    public async Task<IActionResult> SendTemplate([FromBody] SendWhatsAppTemplateRequest request, CancellationToken cancellationToken)
    {
        var parameters = new TemplateSendParameters(
            request.Header is { } h ? new TemplateHeaderParam(h.Kind, h.Text, h.MediaLink) : null,
            request.Body,
            request.Buttons?.Select(b => new TemplateButtonParam(b.Index, b.SubType, b.Text)).ToList());

        var command = new SendTemplateMessageCommand(User.GetTenantId(), request.ToPhone, request.TemplateName, request.LanguageCode, parameters);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Uploads a file to permanent public storage and returns its URL, for use as a template media
    /// header on send. Any authenticated user (agents pick header media in the composer template picker).
    /// </summary>
    [HttpPost("media")]
    [RequestSizeLimit(25_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedia([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Result.Failure(AppErrors.MediaFileRequired).ToActionResult();

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var result = await mediaStorage.UploadAsync(buffer.ToArray(), file.FileName, contentType, cancellationToken);
        return result.ToActionResult();
    }
}
