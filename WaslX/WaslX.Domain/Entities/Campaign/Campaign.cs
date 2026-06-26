using WaslX.Domain.Common;
using System;
using System.Collections.Generic;

namespace WaslX.Domain.Entities
{
    public class Campaign : BaseEntity
    {
        public int TenantId { get; set; }
        public int WaAccountId { get; set; }
        public int CreatedBy { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string MessageBody { get; set; } = string.Empty;
        public string AudienceFilter { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int ReadCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime? ScheduledAt { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;
        public User Creator { get; set; } = null!;
        public ICollection<CampaignRecipient> Recipients { get; set; } = new HashSet<CampaignRecipient>();
        public ICollection<CampaignTag> CampaignTags { get; set; } = new HashSet<CampaignTag>();
    }
}
