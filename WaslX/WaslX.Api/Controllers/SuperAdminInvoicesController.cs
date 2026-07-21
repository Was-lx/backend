using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Platform-owner invoice actions that are not scoped under a tenant route (US-6.3). Cross-tenant;
/// audited via <c>IPlatformAuditService</c> with the calling SuperAdmin as the actor.
/// </summary>
[ApiController]
[Route("api/superadmin/invoices")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminInvoicesController(ISender sender) : ControllerBase
{
    [HttpPost("{invoiceId:int}/mark-paid")]
    public async Task<IActionResult> MarkPaid(int invoiceId, CancellationToken cancellationToken) =>
        (await sender.Send(new MarkInvoicePaidCommand(invoiceId, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
