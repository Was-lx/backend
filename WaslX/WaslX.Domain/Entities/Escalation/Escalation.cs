using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
using WaslX.Domain.SharedEnums;

namespace WaslX.Domain.Entities
{
    public class Escalation : BaseEntity
    {
        public int TenantId { get; set; }
        public int ConversationId { get; set; }
        public int? AssignedUserId { get; set; }
        public int? SuggestedAssigneeId { get; set; }
        public string SuggestedReason { get; set; } = string.Empty;
        public EscalationStatus Status { get; set; }

        // US-4.5 Screening fields
        public EscalationMode? ModeAtDecision { get; set; }
        public int? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedAtUtc { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? AssignedAtUtc { get; set; }
        public string? OverrideReason { get; set; }

        // US-4.4 Auto-escalation fields
        public int? MessageClassificationId { get; set; }
        public int? MessageId { get; set; }
        public string Priority { get; set; } = "normal";
        public string Sentiment { get; set; } = "neutral";
        public string EscalationReason { get; set; } = string.Empty;
        public DateTime? NotifiedAtUtc { get; set; }
        public bool CreatedBySystem { get; set; }

        // US-4.5 Scoring persistence
        public string Topic { get; set; } = "general";
        public decimal? SuggestedScore { get; set; }
        public DateTime? RecommendationGeneratedAtUtc { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public Conversation Conversation { get; set; } = null!;
        public User? AssignedUser { get; set; }
        public User? SuggestedAssignee { get; set; }
        public User? ConfirmedByUser { get; set; }
        public User? AssignedTo { get; set; }
        public MessageClassification? MessageClassification { get; set; }
        public Message? Message { get; set; }
        public ICollection<EscalationCandidateSnapshot> CandidateSnapshots { get; set; } = new List<EscalationCandidateSnapshot>();
    }
}
