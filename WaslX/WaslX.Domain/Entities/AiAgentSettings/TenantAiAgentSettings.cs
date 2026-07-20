using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    public class TenantAiAgentSettings : BaseEntity
    {
        public int TenantId { get; set; }
        public bool Enabled { get; set; }
        public string PersonaName { get; set; } = string.Empty;
        public string ToneInstructions { get; set; } = string.Empty;
        public decimal HandoffThreshold { get; set; }
        
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public User? UpdatedByUser { get; set; }
    }
}
