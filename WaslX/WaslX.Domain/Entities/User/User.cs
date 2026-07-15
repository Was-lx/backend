using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Entities.Base;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    public class User : Account
    {
        public int TenantId { get; set; }
        public int RoleId { get; set; }
        public string Name { get; set; } = string.Empty;

        public Tenant Tenant { get; set; } = null!;
        public Role Role { get; set; } = null!;
        public ICollection<UserGroup> UserGroups { get; set; } = new HashSet<UserGroup>();
        public ICollection<AgentPerformance> AgentPerformances { get; set; } = new HashSet<AgentPerformance>();
        public ICollection<Conversation> AssignedConversations { get; set; } = new HashSet<Conversation>();
        public ICollection<Message> SentMessages { get; set; } = new HashSet<Message>();
        public ICollection<InternalNote> InternalNotes { get; set; } = new HashSet<InternalNote>();
        public ICollection<ConversationStageHistory> ConversationStageHistories { get; set; } = new HashSet<ConversationStageHistory>();
        public ICollection<Assignment> Assignments { get; set; } = new HashSet<Assignment>();
        public ICollection<RoutingDecision> RoutingDecisions { get; set; } = new HashSet<RoutingDecision>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>();
        public ICollection<Campaign> CreatedCampaigns { get; set; } = new HashSet<Campaign>();

        // ── Sprint 3: presence & availability (drive Round Robin distribution) ──
        public bool IsOnline { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsOnBreak { get; set; }
        // The tenant's owner (first admin). Their role is locked — it can't be changed or removed.
        public bool IsOwner { get; set; }
        public ICollection<AgentChannelAccess> AgentChannelAccesses { get; set; } = new HashSet<AgentChannelAccess>();
        public ICollection<AgentWhatsAppDistribution> AgentWhatsAppDistributions { get; set; } = new HashSet<AgentWhatsAppDistribution>();
        public ICollection<AgentShift> AgentShifts { get; set; } = new HashSet<AgentShift>();
    }
}
