using WaslX.Application.Features.Billing.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Billing;

public interface ISubscriptionService
{
    Task<Result<SubscriptionResponse>> GetMineAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Subscribe/upgrade to a plan. Runs the SIMULATED payment, then activates immediately.</summary>
    Task<Result<SubscriptionResponse>> UpgradeAsync(int tenantId, UpgradeInput input, CancellationToken cancellationToken = default);

    /// <summary>Stop auto-renew. Access continues until the end of the current period; data is kept.</summary>
    Task<Result> CancelAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Save / replace the tenant's card (simulated — stores brand + last four only).</summary>
    Task<Result<PaymentMethodDto>> SetPaymentMethodAsync(int tenantId, AddCardInput input, CancellationToken cancellationToken = default);
}
