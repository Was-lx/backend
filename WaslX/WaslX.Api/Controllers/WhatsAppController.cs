using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.WhatsApp.ConnectAccount;
using WaslX.Application.Features.WhatsApp.Disconnect;
using WaslX.Application.Features.WhatsApp.GetAccount;
using WaslX.Application.Features.WhatsApp.SendTemplateMessage;
using WaslX.Application.Features.WhatsApp.SendTextMessage;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhatsAppController(ISender sender) : ControllerBase
{
    /// <summary>Completes the Facebook Login for Business flow and connects the tenant's WhatsApp number.</summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectWhatsAppRequest request, CancellationToken cancellationToken)
    {
        var command = new ConnectWhatsAppAccountCommand(User.GetTenantId(), request.AuthorizationCode, request.WabaId);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    /// <summary>Returns the tenant's WhatsApp connection status (never the access token).</summary>
    [HttpGet("account")]
    public async Task<IActionResult> GetAccount(CancellationToken cancellationToken) =>
        (await sender.Send(new GetWhatsAppAccountQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

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
        var command = new SendTemplateMessageCommand(User.GetTenantId(), request.ToPhone, request.TemplateName, request.LanguageCode);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }
}
