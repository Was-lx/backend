using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A global platform secret / integration credential (Sprint 6 — Platform Owner console). Values are
    /// stored encrypted in <see cref="EncryptedValue"/> and never returned raw; <see cref="Masked"/> holds
    /// a display-safe preview. Global — no tenant scope.
    /// </summary>
    public class PlatformCredential : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string EncryptedValue { get; set; } = string.Empty;
        public string Masked { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? RotatedAt { get; set; }
    }
}
