using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A platform-wide announcement (Sprint 6 — Platform Owner console). Broadcast from the platform console
    /// to tenants. <see cref="Audience"/> selects who sees it (e.g. all tenants or a targeted subset listed in
    /// <see cref="TargetTenantIds"/>). <see cref="Body"/> is long-form text. <see cref="CreatedByPlatformUserId"/>
    /// tracks the authoring SuperAdmin (a platform actor by string id — no domain <c>User</c> row / no tenant).
    /// </summary>
    public class Announcement : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string? TargetTenantIds { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string CreatedByPlatformUserId { get; set; } = string.Empty;
    }
}
