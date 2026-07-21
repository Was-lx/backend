using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A per-user, tenant-scoped in-app notification (e.g. a conversation was assigned to the user).
    /// Delivered in real-time over SignalR and persisted so it survives page reloads.
    /// </summary>
    public class Notification : BaseEntity
    {
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public bool IsRead { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
