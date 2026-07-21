using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// Global, cross-tenant audit read (US-6.9). Read-only, immutable: exposes ONLY a filtered, paged GET
/// over the platform audit trail — no create/update/delete. Guarded by the SuperAdmin role.
/// </summary>
[ApiController]
[Route("api/superadmin/audit-logs")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminAuditController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? actor,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int? tenantId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetPlatformAuditLogsQuery(
                new PlatformAuditQuery(actor, action, entityType, tenantId, from, to, search, page, pageSize)),
            cancellationToken)).ToActionResult();
}
