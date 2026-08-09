using System;
using System.Collections.Generic;

namespace WaslX.Application.Features.Escalation.Models
{
    public sealed class EscalationCandidateScore
    {
        public int UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public decimal PerformanceScore { get; init; }
        public decimal ResponseSpeedScore { get; init; }
        public decimal WorkloadScore { get; init; }
        public decimal TotalScore { get; init; }
        public int ActiveChats { get; init; }
        public string Status { get; set; } = "Eligible"; // "Eligible", "Overloaded"
    }

    public sealed class EscalationScoringResult
    {
        public int EscalationId { get; init; }
        public int? SuggestedAssigneeId { get; set; }
        public string SuggestedAssigneeName { get; init; } = string.Empty;
        public decimal Score { get; init; }
        public string Reason { get; init; } = string.Empty;
        public IReadOnlyList<EscalationCandidateScore> Candidates { get; init; }
            = Array.Empty<EscalationCandidateScore>();
    }
}
