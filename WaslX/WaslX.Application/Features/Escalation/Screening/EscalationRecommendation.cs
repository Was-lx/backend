namespace WaslX.Application.Features.Escalation.Screening
{
    public sealed class EscalationRecommendation
    {
        public int EscalationId { get; init; }
        public int ConversationId { get; init; }
        public int? SuggestedAssigneeId { get; init; }
        public string? SuggestedAssigneeName { get; init; }
        public string Reason { get; init; } = string.Empty;
        public decimal? Score { get; init; }
        public string Mode { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int? AssignedToId { get; init; }
        public string? AssignedToName { get; init; }
        public int? PreviousOwnerId { get; init; }
        public string? PreviousOwnerName { get; init; }
        public string? OverrideReason { get; init; }
        public DateTime? OwnershipTransferredAtUtc { get; init; }
        public DateTime? ConfirmedAtUtc { get; init; }
        public DateTime? AssignedAtUtc { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
