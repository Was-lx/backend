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

        /// <summary>
        /// Meta's message id returned when this recipient's template was sent. Used by the WhatsApp
        /// status webhook to correlate delivered/read/failed callbacks back to this recipient.
        /// </summary>
        public string? WhatsAppMessageId { get; set; }

        public Campaign Campaign { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
    }
}
