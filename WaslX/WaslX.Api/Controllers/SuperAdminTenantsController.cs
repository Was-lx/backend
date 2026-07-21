using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Application.Features.Tenants;
using WaslX.Application.Features.Tenants.Dtos;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/superadmin/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminTenantsController(ISender sender) : ControllerBase
{
    // ── Tenant list & provisioning ──
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetTenantsQuery(), cancellationToken)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTenantDetailQuery(id), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SuperAdminCreateTenantInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateTenantCommand(input), cancellationToken)).ToActionResult();

    // ── Tenant lifecycle (US-6.2) ──
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Configure(int id, [FromBody] ConfigureTenantInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new ConfigureTenantCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetTenantStatusRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetTenantStatusCommand(id, request.Status, Actor()), cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new SoftDeleteTenantCommand(id, Actor()), cancellationToken)).ToActionResult();

    // ── Billing / invoicing (US-6.3) ──
    [HttpPost("{id:int}/plan")]
    public async Task<IActionResult> ChangePlan(int id, [FromBody] ChangeTenantPlanInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new ChangeTenantPlanCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    [HttpGet("{id:int}/invoices")]
    public async Task<IActionResult> GetInvoices(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTenantInvoicesQuery(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:int}/invoices")]
    public async Task<IActionResult> GenerateInvoice(int id, [FromBody] GenerateInvoiceInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new GenerateInvoiceCommand(id, input, Actor()), cancellationToken)).ToActionResult();

    private PlatformActor Actor() =>
        new(User.GetUserId() ?? string.Empty, User.GetEmail() ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());
}
