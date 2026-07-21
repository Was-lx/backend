namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>
/// Cross-tenant AI cost snapshot (US-6.5). Aggregates <c>AiUsageRecord</c> rows across every tenant
/// (no tenant filter). When the AI pipeline has not written any usage yet (it ships in a later sprint),
/// every total is zero and the collections are empty — never an error.
/// </summary>
public record AiCostResponse(
    decimal TotalCostUsd,
    long TotalTokens,
    IReadOnlyList<TenantAiCostRow> PerTenant,
    IReadOnlyList<AiCostByModel> ByModel,
    IReadOnlyList<AiCostByComponent> ByComponent,
    IReadOnlyList<AiCostDailyPoint> Daily,
    IReadOnlyList<BudgetBreach> Breaches);

/// <summary>Per-tenant AI cost breakdown row.</summary>
public record TenantAiCostRow(int TenantId, string TenantName, decimal CostUsd, long Tokens);

/// <summary>AI cost grouped by model (e.g. gpt-4.1-mini, text-embedding-3-large).</summary>
public record AiCostByModel(string Model, decimal CostUsd, long Tokens);

/// <summary>AI cost grouped by pipeline component (RAG, classification, agent, summary…).</summary>
public record AiCostByComponent(string Component, decimal CostUsd, long Tokens);

/// <summary>A single day in the platform-wide AI cost time series.</summary>
public record AiCostDailyPoint(DateTime Date, decimal CostUsd, long Tokens);

/// <summary>
/// A budget alert whose accrued spend (over its period + scope) has met or exceeded its threshold.
/// Surfaced in the cost dashboard so the Platform Owner can act.
/// </summary>
public record BudgetBreach(
    int AlertId,
    int? TenantId,
    string Scope,
    string Period,
    decimal ThresholdUsd,
    decimal ActualUsd);

/// <summary>Optional date window for the AI cost daily series (inclusive). Defaults to the last 30 days.</summary>
public record AiCostQuery(DateTime? From, DateTime? To);

// ── Budget-alert CRUD ──

/// <summary>A configured spending threshold (US-6.5).</summary>
public record BudgetAlertResponse(
    int Id,
    int? TenantId,
    string Scope,
    decimal ThresholdUsd,
    string Period,
    bool IsActive,
    DateTime? LastTriggeredAt,
    string? NotifyEmail,
    DateTime CreatedAt);

/// <summary>Create a budget alert. <paramref name="TenantId"/> null = a global (platform-wide) alert.</summary>
public record CreateBudgetAlertInput(
    int? TenantId,
    string Scope,
    decimal ThresholdUsd,
    string Period,
    bool IsActive,
    string? NotifyEmail);

/// <summary>Update an existing budget alert (scope + tenant are fixed at creation).</summary>
public record UpdateBudgetAlertInput(
    string Scope,
    decimal ThresholdUsd,
    string Period,
    bool IsActive,
    string? NotifyEmail);
