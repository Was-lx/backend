using System;

namespace WaslX.Application.Features.Escalation.Models
{
    public sealed class EscalationScoringInput
    {
        public int TenantId { get; init; }
        public int ConversationId { get; init; }
        public int EscalationId { get; init; }
        public string Topic { get; init; } = "general";
        public string Sentiment { get; init; } = "neutral";
        public string Priority { get; init; } = "normal";
        public string EscalationReason { get; init; } = string.Empty;
    }
}
