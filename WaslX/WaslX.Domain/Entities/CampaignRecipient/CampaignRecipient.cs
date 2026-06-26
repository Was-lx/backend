using WaslX.Domain.Common;
using System;

namespace WaslX.Domain.Entities
{
    public class CampaignRecipient : BaseEntity
    {
        public int CampaignId { get; set; }
        public int CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public Campaign Campaign { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
    }
}
