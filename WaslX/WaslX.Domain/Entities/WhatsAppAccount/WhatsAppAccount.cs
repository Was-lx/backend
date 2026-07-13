using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class WhatsAppAccount : BaseEntity
    {
        public int TenantId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public WhatsAppAccountStatus Status { get; set; }
        public DateTime ConnectedAt { get; set; }

        public string PhoneNumberId { get; set; } = string.Empty;

        public string WhatsAppBusinessAccountId { get; set; } = string.Empty;
        public DateTime? TokenExpiresAt { get; set; }
        public Tenant Tenant { get; set; } = null!;
        public ICollection<Conversation> Conversations { get; set; } = new HashSet<Conversation>();
        public ICollection<Campaign> Campaigns { get; set; } = new HashSet<Campaign>();
    }
}
