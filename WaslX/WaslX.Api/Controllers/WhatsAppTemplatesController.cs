using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Authorization;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.WhatsApp.Templates.CreateTemplate;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Application.Features.WhatsApp.Templates.GetTemplates;
using WaslX.Domain.Authorization;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/whatsapp/templates")]
[Authorize]
public class WhatsAppTemplatesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lists the tenant's message templates from Meta, optionally filtered by status. Available to any
    /// authenticated user because agents need the APPROVED list for the composer template picker.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplates([FromQuery] string? status, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTemplatesQuery(User.GetTenantId(), status), cancellationToken)).ToActionResult();

    /// <summary>Creates a message template on Meta (Marketing / Utility / Authentication). Managers/Admins only.</summary>
    [HttpPost]
    [HasPermission(Permissions.TemplateManage)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var buttons = (request.Buttons ?? [])
            .Select(b => new TemplateButtonInput(b.Type, b.Text, b.Url))
            .ToList();

        var command = new CreateTemplateCommand(
            User.GetTenantId(), request.Name, request.Category, request.Language,
            request.HeaderText, request.BodyText, request.FooterText, buttons, request.AllowCategoryChange);

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }
}
