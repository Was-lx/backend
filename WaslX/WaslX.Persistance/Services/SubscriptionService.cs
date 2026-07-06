using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Features.Billing.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Subscription & billing. The payment step is SIMULATED — it validates the card shape,
/// stores only the last four digits, marks an invoice paid, and activates the plan. A real
/// gateway plugs in behind SetPaymentMethodAsync / the "charge" in UpgradeAsync later.
/// </summary>
internal sealed class SubscriptionService(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : ISubscriptionService
{
    public async Task<Result<SubscriptionResponse>> GetMineAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.AsNoTracking().Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure<SubscriptionResponse>(AppErrors.TenantNotFound);
        return Result.Success(await BuildAsync(tenant, cancellationToken));
    }

    public async Task<Result<SubscriptionResponse>> UpgradeAsync(int tenantId, UpgradeInput input, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure<SubscriptionResponse>(AppErrors.TenantNotFound);

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == input.PlanId, cancellationToken);
        if (plan is null || !plan.IsActive) return Result.Failure<SubscriptionResponse>(AppErrors.PlanNotFound);
        if (plan.IsCustom) return Result.Failure<SubscriptionResponse>(new Error("Billing.CustomPlan", "This plan is sales-only — please contact us", 400));

        var cycle = Enum.TryParse<BillingCycle>(input.BillingCycle, true, out var c) ? c : BillingCycle.Monthly;

        var hasCard = await db.PaymentMethods.AnyAsync(p => p.TenantId == tenantId, cancellationToken);
        if (!hasCard) return Result.Failure<SubscriptionResponse>(AppErrors.PaymentMethodRequired);

        var amount = cycle == BillingCycle.Yearly ? (plan.PriceYearly ?? plan.Price) * 12 : plan.Price;

        // Simulated charge → always succeeds when a card is on file.
        var now = DateTime.UtcNow;
        db.Invoices.Add(new Invoice
        {
            TenantId = tenantId,
            Amount = amount,
            Status = InvoiceStatus.Paid,
            IssuedAt = now,
            DueDate = now,
            PaidAt = now,
        });

        tenant.PlanId = plan.Id;
        tenant.BillingStatus = BillingStatus.Active;
        tenant.SelectedBillingCycle = cycle;
        tenant.CurrentPeriodEnd = cycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1);
        tenant.TrialEndsAt = null;
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Tenants.AsNoTracking().Include(t => t.Plan).FirstAsync(t => t.Id == tenantId, cancellationToken);
        return Result.Success(await BuildAsync(reloaded, cancellationToken));
    }

    public async Task<Result> CancelAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(AppErrors.TenantNotFound);

        // Stop auto-renew. Access stays until CurrentPeriodEnd; data is kept.
        tenant.BillingStatus = BillingStatus.Cancelled;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PaymentMethodDto>> SetPaymentMethodAsync(int tenantId, AddCardInput input, CancellationToken cancellationToken = default)
    {
        var digits = new string((input.Number ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length is < 12 or > 19)
            return Result.Failure<PaymentMethodDto>(AppErrors.PaymentDeclined);

        var now = DateTime.UtcNow;
        var expValid = input.ExpMonth is >= 1 and <= 12 &&
                       (input.ExpYear > now.Year || (input.ExpYear == now.Year && input.ExpMonth >= now.Month));
        if (!expValid)
            return Result.Failure<PaymentMethodDto>(AppErrors.PaymentDeclined);

        // Replace any existing card (single card on file, simulated).
        var existing = await db.PaymentMethods.Where(p => p.TenantId == tenantId).ToListAsync(cancellationToken);
        db.PaymentMethods.RemoveRange(existing);

        var pm = new PaymentMethod
        {
            TenantId = tenantId,
            Brand = DetectBrand(digits),
            Last4 = digits[^4..],
            ExpMonth = input.ExpMonth,
            ExpYear = input.ExpYear,
            HolderName = string.IsNullOrWhiteSpace(input.HolderName) ? null : input.HolderName.Trim(),
            IsDefault = true,
        };
        db.PaymentMethods.Add(pm);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentMethodDto(pm.Id, pm.Brand, pm.Last4, pm.ExpMonth, pm.ExpYear, pm.HolderName));
    }

    private async Task<SubscriptionResponse> BuildAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var agents = await userManager.Users.CountAsync(u => u.TenantId == tenant.Id, cancellationToken);
        var numbers = await db.WhatsAppAccounts.CountAsync(w => w.TenantId == tenant.Id, cancellationToken);

        var card = await db.PaymentMethods.AsNoTracking()
            .Where(p => p.TenantId == tenant.Id)
            .OrderByDescending(p => p.IsDefault).ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenant.Id)
            .OrderByDescending(i => i.IssuedAt)
            .Take(20)
            .Select(i => new InvoiceDto(i.Id, i.Amount, i.Status.ToString(), i.IssuedAt, i.PaidAt))
            .ToListAsync(cancellationToken);

        var cycle = tenant.SelectedBillingCycle ?? tenant.Plan.BillingCycle;
        var price = cycle == BillingCycle.Yearly ? (tenant.Plan.PriceYearly ?? tenant.Plan.Price) : tenant.Plan.Price;

        int? trialDaysLeft = tenant.TrialEndsAt is { } end
            ? Math.Max(0, (int)Math.Ceiling((end - DateTime.UtcNow).TotalDays))
            : null;

        var status = tenant.BillingStatus == BillingStatus.Trial && tenant.TrialEndsAt is { } t && t < DateTime.UtcNow
            ? "PastDue"
            : tenant.BillingStatus.ToString();

        return new SubscriptionResponse(
            status,
            tenant.Plan.Id, tenant.Plan.Code, tenant.Plan.Name, price, cycle.ToString(),
            tenant.TrialEndsAt, trialDaysLeft, tenant.CurrentPeriodEnd,
            new UsageDto(agents, tenant.Plan.MaxAgents, numbers, tenant.Plan.MaxNumbers, tenant.Plan.MsgQuota, tenant.Plan.AiQuota),
            card is null ? null : new PaymentMethodDto(card.Id, card.Brand, card.Last4, card.ExpMonth, card.ExpYear, card.HolderName),
            invoices);
    }

    private static string DetectBrand(string digits) => digits.Length == 0 ? "Card" : digits[0] switch
    {
        '4' => "Visa",
        '5' => "Mastercard",
        '3' => "Amex",
        '6' => "Discover",
        _ => "Card"
    };
}
