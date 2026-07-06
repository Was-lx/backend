using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Billing;
using WaslX.Application.Features.Billing.Dtos;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize(Roles = "Admin")]
public class SubscriptionController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new GetMySubscriptionQuery(tenantId), cancellationToken)).ToActionResult();
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeInput input, CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new UpgradeSubscriptionCommand(tenantId, input), cancellationToken)).ToActionResult();
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new CancelSubscriptionCommand(tenantId), cancellationToken)).ToActionResult();
    }

    [HttpPost("payment-method")]
    public async Task<IActionResult> SetPaymentMethod([FromBody] AddCardInput input, CancellationToken cancellationToken)
    {
        if (User.GetTenantId() is not { } tenantId) return NoTenant();
        return (await sender.Send(new SetPaymentMethodCommand(tenantId, input), cancellationToken)).ToActionResult();
    }

    private IActionResult NoTenant() => BadRequest(new { code = "Tenant.NoContext", description = "This account is not attached to a workspace" });
}
