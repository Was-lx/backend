using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Audit;
using WaslX.Application.Features.Audit.Dtos;
using WaslX.Domain.SharedEnums;

namespace WaslX.Api.Controllers;

/// <summary>
/// Read-only tenant audit trail (FR-AUDIT / US-5.7). Immutable: exposes ONLY a filtered, paged
/// GET — there is deliberately no create/update/delete. Scoped to the caller's workspace.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class AuditController(ISender sender) : ControllerBase
{
    [HttpGet("api/audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? actor,
        [FromQuery] AuditAction? action,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetAuditLogsQuery(
                User.GetTenantId(),
                new AuditQuery(actor, action, entityType, from, to, search, page, pageSize)),
            cancellationToken)).ToActionResult();
}
