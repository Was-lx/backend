namespace WaslX.Application.Features.Billing.Dtos;

public record UsageDto(int AgentsUsed, int MaxAgents, int NumbersUsed, int MaxNumbers, int MsgQuota, int AiQuota);

public record PaymentMethodDto(int Id, string Brand, string Last4, int ExpMonth, int ExpYear, string? HolderName);

public record InvoiceDto(int Id, decimal Amount, string Status, DateTime IssuedAt, DateTime? PaidAt);

public record SubscriptionResponse(
    string Status,                 // Trial / Active / PastDue / Cancelled
    int PlanId,
    string PlanCode,
    string PlanName,
    decimal Price,
    string BillingCycle,
    DateTime? TrialEndsAt,
    int? TrialDaysLeft,
    DateTime? CurrentPeriodEnd,
    UsageDto Usage,
    PaymentMethodDto? PaymentMethod,
    IReadOnlyList<InvoiceDto> Invoices);

/// <summary>Upgrade/subscribe request (monthly | yearly).</summary>
public record UpgradeInput(int PlanId, string BillingCycle);

/// <summary>Simulated card entry — only the last four digits are ever stored.</summary>
public record AddCardInput(string Number, int ExpMonth, int ExpYear, string? HolderName);
