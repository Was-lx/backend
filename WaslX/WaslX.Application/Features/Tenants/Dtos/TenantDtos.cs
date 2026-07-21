namespace WaslX.Application.Features.Tenants.Dtos;

/// <summary>Self-serve sign-up: account details + organization profile, captured before onboarding.</summary>
public record SelfServeSignupInput(
    string FullName,
    string Email,
    string Password,
    string OrgName,
    string? Website,
    string? Industry,
    string? Phone,
    string CustomerType);

/// <summary>SuperAdmin-provisioned tenant + its first Admin user.</summary>
public record SuperAdminCreateTenantInput(
    string OrgName,
    string AdminEmail,
    string AdminFullName,
    int PlanId,
    string? Website,
    string? Industry,
    string? Phone,
    string CustomerType,
    bool StartTrial);

/// <summary>A workspace's own profile (for the settings screen & onboarding resume).</summary>
public record TenantProfileResponse(
    int Id,
    string Name,
    string? Website,
    string? Industry,
    string? PhoneNumber,
    string CustomerType,
    string Status,
    string BillingStatus,
    int PlanId,
    string PlanName,
    DateTime? TrialEndsAt,
    int? TrialDaysLeft,
    DateTime? CurrentPeriodEnd,
    int OnboardingStep,
    bool OnboardingCompleted);

/// <summary>One row of the SuperAdmin tenants list.</summary>
public record TenantSummaryResponse(
    int Id,
    string Name,
    string PlanName,
    string Status,
    string BillingStatus,
    DateTime? TrialEndsAt,
    int UserCount,
    string? AdminEmail,
    DateTime CreatedAt);

public record UpdateTenantProfileInput(
    string Name,
    string? Website,
    string? Industry,
    string? Phone,
    string CustomerType);

/// <summary>
/// Full tenant detail for the SuperAdmin console: profile, plan, lifecycle/billing status, live
/// resource usage vs. the plan's limits, the workspace users and the tenant's invoices. The shape
/// mirrors the frontend TenantDetail view-model exactly (the Overview / Users / Invoices tabs read
/// straight off this object).
/// </summary>
public record TenantDetailResponse(
    int Id,
    string Name,
    string? Website,
    string? Industry,
    string? PhoneNumber,
    string CustomerType,
    string Status,
    string BillingStatus,
    int PlanId,
    string PlanName,
    decimal Price,
    string BillingCycle,
    DateTime? TrialEndsAt,
    int? TrialDaysLeft,
    DateTime? CurrentPeriodEnd,
    DateTime CreatedAt,
    TenantUsageDto Usage,
    decimal Mrr,
    IReadOnlyList<TenantUserRowDto> Users,
    IReadOnlyList<TenantInvoiceDto> Invoices);

/// <summary>Live resource usage vs. the plan's limits, for the tenant-detail overview.</summary>
public record TenantUsageDto(
    int AgentsUsed,
    int MaxAgents,
    int NumbersUsed,
    int MaxNumbers,
    int MsgQuota,
    int AiQuota);

/// <summary>One workspace user in the tenant-detail Users tab.</summary>
public record TenantUserRowDto(
    string Id,
    string Name,
    string Email,
    string Role,
    bool IsActive);

/// <summary>One invoice in the tenant-detail Invoices tab (mirrors the platform PlatformInvoice VM).</summary>
public record TenantInvoiceDto(
    int Id,
    int TenantId,
    string? TenantName,
    decimal Amount,
    string Status,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    DateTime IssuedAt,
    DateTime? DueDate,
    DateTime? PaidAt);

/// <summary>SuperAdmin: configure a tenant — its profile plus which plan it is on.</summary>
public record ConfigureTenantInput(
    string Name,
    string? Website,
    string? Industry,
    string? Phone,
    string CustomerType,
    int PlanId);
