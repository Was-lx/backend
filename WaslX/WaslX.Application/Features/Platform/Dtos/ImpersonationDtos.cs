namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>Request to start an audited impersonation of a tenant workspace (US-6.8).</summary>
public record StartImpersonationInput(int TenantId, string Reason);

/// <summary>A minimal tenant identity returned alongside a minted impersonation token.</summary>
public record ImpersonatedTenantSummary(int TenantId, string Name, string Status);

/// <summary>An audited impersonation session row (Active/Ended/Expired).</summary>
public record ImpersonationSessionResponse(
    int Id,
    string ActorPlatformUserId,
    string ActorEmail,
    int TenantId,
    int? TargetUserId,
    string Reason,
    DateTime StartedAt,
    DateTime ExpiresAt,
    DateTime? EndedAt,
    string Status);

/// <summary>
/// Result of starting an impersonation: a short-lived tenant JWT the console uses to act inside the
/// target workspace, its expiry, the target tenant, and the newly created session row.
/// </summary>
public record StartImpersonationResponse(
    string Token,
    DateTime ExpiresAt,
    ImpersonatedTenantSummary Tenant,
    ImpersonationSessionResponse Session);
