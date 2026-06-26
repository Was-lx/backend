using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class User : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserStatus Status { get; set; }

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
    }
}