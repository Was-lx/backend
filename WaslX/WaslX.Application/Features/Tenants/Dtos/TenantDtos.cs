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
