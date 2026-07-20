using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Conversation : BaseEntity
    {
        public int TenantId { get; set; }
        public int WhatsAppAccountId { get; set; }
        public int CustomerId { get; set; }
        public int? AssignedUserId { get; set; }
        public int? GroupId { get; set; }
        public int? CurrentStageId { get; set; }
        public ConversationStatus Status { get; set; }
        public ConversationPriority Priority { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime? LastCustomerMessageAt { get; set; }
        public DateTime? WindowExpiresAt { get; set; }
        public ConversationWindowType WindowType { get; set; } = ConversationWindowType.None;
        public DateTime? LastReadAt { get; set; }
        public bool IsDeleted { get; set; }
        
        // US-4.6 AI Agent
        public bool HandledByAi { get; set; }
        public AiConversationMode AiMode { get; set; } = AiConversationMode.Active;

        // US-4.4 Auto-escalation flags
        public bool IsEscalated { get; set; }
        public DateTime? EscalatedAtUtc { get; set; }
        public string? EscalationReason { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public User? AssignedUser { get; set; }
        public Group? Group { get; set; }
        public Stage? CurrentStage { get; set; }
        public ICollection<Message> Messages { get; set; } = new HashSet<Message>();
        public ICollection<InternalNote> InternalNotes { get; set; } = new HashSet<InternalNote>();
        public ICollection<ConversationTag> ConversationTags { get; set; } = new HashSet<ConversationTag>();
        public ICollection<ConversationStageHistory> ConversationStageHistories { get; set; } = new HashSet<ConversationStageHistory>();
        public ICollection<Assignment> Assignments { get; set; } = new HashSet<Assignment>();
        public ICollection<RoutingDecision> RoutingDecisions { get; set; } = new HashSet<RoutingDecision>();
    }
}
