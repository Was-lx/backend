using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Billing.Dtos;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Platform-owner billing &amp; invoicing across all tenants (US-6.3). Deliberately CROSS-TENANT — no
/// tenant filter, because the only caller is the SuperAdmin console guarded by
/// <c>[Authorize(Roles = "SuperAdmin")]</c>. Reuses the <see cref="Invoice"/> entity and the
/// <see cref="InvoiceDto"/> shape from the tenant billing feature. Every mutation is audited.
/// </summary>
internal sealed class SuperAdminBillingService(
    ApplicationDbContext db,
    IPlatformAuditService audit) : ISuperAdminBillingService
{
    public async Task<Result> ChangePlanAsync(int tenantId, ChangeTenantPlanInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == input.PlanId, cancellationToken);
        if (plan is null) return Result.Failure(AppErrors.PlanNotFound);

        var billingStatus = BillingStatus.Active;
        if (!string.IsNullOrWhiteSpace(input.BillingStatus))
        {
            if (!Enum.TryParse(input.BillingStatus, true, out billingStatus))
                return Result.Failure(AppErrors.InvalidBillingStatus);
        }

        var previousPlanId = tenant.PlanId;
        var now = DateTime.UtcNow;

        tenant.PlanId = plan.Id;
        tenant.BillingStatus = billingStatus;
        // Keep the lifecycle coherent: a paid/active assignment ends the trial and opens a period.
        if (billingStatus == BillingStatus.Active)
        {
            tenant.TrialEndsAt = null;
            tenant.CurrentPeriodEnd ??= now.AddMonths(1);
        }
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "TenantPlanChanged", "Tenant", tenant.Id.ToString(),
            tenant.Id, $"plan {previousPlanId}→{plan.Id}; billing={billingStatus}", actor.Ip, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InvoiceDto>>> GetInvoicesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);
        if (!tenantExists) return Result.Failure<IReadOnlyList<InvoiceDto>>(AppErrors.TenantNotFound);

        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssuedAt).ThenByDescending(i => i.Id)
            .Select(i => new InvoiceDto(i.Id, i.Amount, i.Status.ToString(), i.IssuedAt, i.PaidAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<InvoiceDto>>(invoices);
    }

    public async Task<Result<InvoiceDto>> GenerateInvoiceAsync(int tenantId, GenerateInvoiceInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);
        if (!tenantExists) return Result.Failure<InvoiceDto>(AppErrors.TenantNotFound);

        if (input.Amount <= 0) return Result.Failure<InvoiceDto>(AppErrors.InvalidInvoiceAmount);

        var status = InvoiceStatus.Pending;
        if (!string.IsNullOrWhiteSpace(input.Status))
        {
            if (!Enum.TryParse(input.Status, true, out status))
                return Result.Failure<InvoiceDto>(AppErrors.InvalidInvoiceStatus);
        }

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            TenantId = tenantId,
            Amount = input.Amount,
            Status = status,
            IssuedAt = now,
            DueDate = input.DueDate ?? now.AddDays(14),
            PaidAt = status == InvoiceStatus.Paid ? now : null,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "InvoiceGenerated", "Invoice", invoice.Id.ToString(),
            tenantId, $"amount={invoice.Amount}; status={status}", actor.Ip, cancellationToken);

        return Result.Success(new InvoiceDto(invoice.Id, invoice.Amount, invoice.Status.ToString(), invoice.IssuedAt, invoice.PaidAt));
    }

    public async Task<Result<InvoiceDto>> MarkPaidAsync(int invoiceId, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null) return Result.Failure<InvoiceDto>(AppErrors.InvoiceNotFound);
        if (invoice.Status == InvoiceStatus.Paid) return Result.Failure<InvoiceDto>(AppErrors.InvoiceAlreadyPaid);

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "InvoiceMarkedPaid", "Invoice", invoice.Id.ToString(),
            invoice.TenantId, $"amount={invoice.Amount}", actor.Ip, cancellationToken);

        return Result.Success(new InvoiceDto(invoice.Id, invoice.Amount, invoice.Status.ToString(), invoice.IssuedAt, invoice.PaidAt));
    }
}
