using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A global platform configuration key/value (Sprint 6 — Platform Owner console). <see cref="ValueType"/>
    /// records how <see cref="Value"/> should be interpreted (e.g. string, int, bool, json). Global — no
    /// tenant scope. <see cref="Key"/> is unique.
    /// </summary>
    public class PlatformSetting : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
