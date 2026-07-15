namespace WaslX.Application.Abstractions.Identity;

/// <summary>
/// Bridges the dual user model: the API authenticates via ASP.NET Identity (ApplicationUser, GUID id),
/// but Conversation.AssignedUserId and the Sprint-3 join tables reference the domain <c>User</c> (int id,
/// 'users' table), linked to the Identity user by tenant + email. This resolves (find-or-create) that
/// domain user id so it can be embedded in the JWT for inbox scoping / assignment.
/// </summary>
public interface IDomainUserDirectory
{
    Task<int> GetOrCreateDomainUserIdAsync(int tenantId, string email, string? displayName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only check of whether the domain user (tenant + email) is the workspace owner.
    /// Returns false when no domain row exists yet — it never creates one.
    /// </summary>
    Task<bool> IsOwnerAsync(int tenantId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the domain user (tenant + email) as the workspace owner, creating the domain row first
    /// via the find-or-create path if it doesn't exist yet. Idempotent.
    /// </summary>
    Task EnsureOwnerAsync(int tenantId, string email, string? displayName = null, CancellationToken cancellationToken = default);
}
