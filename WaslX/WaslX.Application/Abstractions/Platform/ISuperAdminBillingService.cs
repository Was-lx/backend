using WaslX.Application.Features.Billing.Dtos;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Platform-owner billing &amp; invoicing across all tenants (US-6.3). Cross-tenant by nature — only
/// reached through the SuperAdmin console (<c>[Authorize(Roles = "SuperAdmin")]</c>). Reuses the
/// <see cref="InvoiceDto"/> shape from the tenant-facing billing feature. Every mutation is written
/// to the platform audit trail.
/// </summary>
public interface ISuperAdminBillingService
{
    /// <summary>Assign/change a tenant's plan and billing state.</summary>
    Task<Result> ChangePlanAsync(int tenantId, ChangeTenantPlanInput input, PlatformActor actor, CancellationToken cancellationToken = default);

    /// <summary>Read a tenant's invoices (cross-tenant, newest first).</summary>
    Task<Result<IReadOnlyList<InvoiceDto>>> GetInvoicesAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Manually generate an invoice for a tenant.</summary>
    Task<Result<InvoiceDto>> GenerateInvoiceAsync(int tenantId, GenerateInvoiceInput input, PlatformActor actor, CancellationToken cancellationToken = default);

    /// <summary>Mark an invoice paid.</summary>
    Task<Result<InvoiceDto>> MarkPaidAsync(int invoiceId, PlatformActor actor, CancellationToken cancellationToken = default);
}
