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

        // ── Sprint 3: friendly label + distribution configuration (set in the connect wizard step 1) ──
        public string PlatformName { get; set; } = string.Empty;
        public DistributionMode DistributionMode { get; set; } = DistributionMode.RoundRobin;
        public bool DistributeToOffline { get; set; } = true;   // Round Robin: route to agents even if logged out
        public bool ReassignOnOffline { get; set; }             // Round Robin: reassign an agent's open chats when they go offline
        public int? StartingGroupId { get; set; }               // group whose pipeline a new conversation starts in
        public Group? StartingGroup { get; set; }
        public ICollection<ChannelWhatsAppAccount> ChannelWhatsAppAccounts { get; set; } = new HashSet<ChannelWhatsAppAccount>();
        public ICollection<AgentWhatsAppDistribution> AgentWhatsAppDistributions { get; set; } = new HashSet<AgentWhatsAppDistribution>();
    }
}
