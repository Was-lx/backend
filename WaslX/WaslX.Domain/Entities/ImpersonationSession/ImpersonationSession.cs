using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// An audited SuperAdmin impersonation session (Sprint 6 — Platform Owner console). A platform actor
    /// (tracked by string id + email — a SuperAdmin has no domain <c>User</c> row and no tenant) temporarily
    /// acts inside a tenant, optionally as a specific target user, for a stated reason and bounded window.
    /// <see cref="TenantId"/> is a plain int column (indexed) noting which tenant is being impersonated;
    /// deliberately no FK / navigation. <see cref="Status"/> is stored as a string (Active/Ended/Expired).
    /// </summary>
    public class ImpersonationSession : BaseEntity
    {
        public string ActorPlatformUserId { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public int? TargetUserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
