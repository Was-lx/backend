using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A feature toggle (Sprint 6 — Platform Owner console). <see cref="TenantId"/> is a plain nullable
    /// column: a single null-tenant row is the global default for a <see cref="Key"/>, and per-tenant rows
    /// override it. The (Key, TenantId) pair is unique. Deliberately no FK / navigation.
    /// </summary>
    public class FeatureFlag : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TenantId { get; set; }
        public bool IsEnabled { get; set; }
    }
}
