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

        // ── WhatsApp 24-hour customer-service window ──
        // Timestamp of the last INBOUND (customer) message. Every customer reply resets this, which
        // in turn resets ServiceWindowExpiresAt. Agent/template sends never touch it. NULL = the
        // customer has never messaged, so the window is closed (free text blocked, templates only).
        public DateTime? LastCustomerMessageAt { get; set; }

        // LastCustomerMessageAt + 24 hours. When Now >= this, the conversation window is Closed and
        // only approved template messages may be sent (enforced in WhatsAppService.SendAsync).
        public DateTime? ServiceWindowExpiresAt { get; set; }

        // Read-cursor for the shared inbox: inbound (Customer) messages newer than this are "unread".
        // Null = never opened, so everything counts as unread until the first mark-read.
        public DateTime? LastReadAt { get; set; }
        // Soft-delete: hidden from the inbox and never reused for a new inbound message from the
        // same customer (a fresh message starts a brand-new conversation instead).
        public bool IsDeleted { get; set; }

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
