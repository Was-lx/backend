using WaslX.Domain.Common;
using WaslX.Domain.SharedEnums;

namespace WaslX.Domain.Entities
{
    public class TenantEscalationSettings : BaseEntity
    {
        public int TenantId { get; set; }
        public EscalationMode Mode { get; set; } = EscalationMode.Recommend;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public User? UpdatedByUser { get; set; }
    }
}