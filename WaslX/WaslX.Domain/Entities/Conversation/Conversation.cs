using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Conversation : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WhatsAppAccountId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? AssignedUserId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? CurrentStageId { get; set; }
        public ConversationStatus Status { get; set; }
        public ConversationPriority Priority { get; set; }
        public DateTime? LastMessageAt { get; set; }

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