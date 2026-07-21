using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// Global, immutable platform-level audit trail (Sprint 6 — Platform Owner / Super Admin console).
    /// Unlike the tenant-scoped <see cref="AuditLog"/>, this is NOT filtered by a tenant and records the
    /// actions of a platform actor (a SuperAdmin Identity user, tracked by string id + email — a SuperAdmin
    /// has no domain <c>User</c> row and no tenant). <see cref="TenantId"/> is a plain nullable column that
    /// merely notes which tenant an action touched (e.g. suspend / impersonate); it is deliberately not a
    /// required FK and there is no tenant navigation. Rows are only ever appended.
    /// </summary>
    public class PlatformAuditLog : BaseEntity
    {
        public string ActorPlatformUserId { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public int? TenantId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
    }
}
