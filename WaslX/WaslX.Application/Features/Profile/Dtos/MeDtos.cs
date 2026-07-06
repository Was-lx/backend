namespace WaslX.Application.Features.Profile.Dtos;

/// <summary>The authenticated user's own profile + workspace + resolved permissions (drives UI gating).</summary>
public record MeResponse(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    int? TenantId,
    string? TenantName,
    IReadOnlyList<string> Permissions,
    IReadOnlyDictionary<string, string> Scopes,
    bool OnboardingCompleted,
    int OnboardingStep);

public record UpdateMeInput(string FullName, string? Phone);
