namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>
/// A platform-wide announcement (US-6.10). <c>Audience</c> is one of AllTenants | Plan | SpecificTenants.
/// For SpecificTenants, <c>TargetIds</c> holds tenant ids; for Plan, it holds plan ids.
/// </summary>
public record AnnouncementResponse(
    int Id,
    string Title,
    string Body,
    string Severity,
    string Audience,
    IReadOnlyList<int> TargetIds,
    DateTime? PublishedAt,
    DateTime? ExpiresAt,
    bool IsActive,
    string CreatedByPlatformUserId,
    DateTime CreatedAt);

/// <summary>
/// Create a draft announcement. <c>Audience</c> ∈ {AllTenants, Plan, SpecificTenants}; <c>TargetIds</c>
/// carries tenant ids (SpecificTenants) or plan ids (Plan) and is ignored for AllTenants.
/// </summary>
public record CreateAnnouncementInput(
    string Title,
    string Body,
    string? Severity,
    string Audience,
    IReadOnlyList<int>? TargetIds,
    DateTime? ExpiresAt);
