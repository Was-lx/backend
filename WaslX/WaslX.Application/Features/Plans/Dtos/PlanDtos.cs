namespace WaslX.Application.Features.Plans.Dtos;

public record PlanResponse(
    int Id,
    string Code,
    string Name,
    string? Tagline,
    decimal Price,
    decimal? PriceYearly,
    string BillingCycle,
    int MaxAgents,
    int MaxNumbers,
    int MsgQuota,
    int AiQuota,
    int TrialDays,
    bool IsActive,
    bool IsPublic,
    bool IsCustom,
    int SortOrder,
    IReadOnlyList<string> Features);

public record UpsertPlanRequest(
    string Code,
    string Name,
    string? Tagline,
    decimal Price,
    decimal? PriceYearly,
    string BillingCycle,
    int MaxAgents,
    int MaxNumbers,
    int MsgQuota,
    int AiQuota,
    int TrialDays,
    bool IsActive,
    bool IsPublic,
    bool IsCustom,
    int SortOrder,
    List<string> Features);
