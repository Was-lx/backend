namespace WaslX.Application.Abstractions.AI;

/// <summary>
/// Guards against unbounded AI spend: checks whether a tenant is still within its subscription
/// plan's monthly AI quota BEFORE an expensive classification/generation call is made. This is a
/// pre-call gate, unlike AiUsageRecord-based cost reporting which only observes spend after it
/// already happened — without this, nothing stops an inbound message flood from generating unlimited
/// billable LLM calls regardless of the tenant's plan.
/// </summary>
public interface IAiUsageQuotaService
{
    Task<bool> IsWithinQuotaAsync(int tenantId, CancellationToken cancellationToken = default);
}
