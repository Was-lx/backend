using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    public class EscalationCandidateSnapshot : BaseEntity
    {
        public int EscalationId { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public decimal OverallScore { get; set; }
        public decimal PerformanceScore { get; set; }
        public decimal ResponseSpeedScore { get; set; }
        public decimal WorkloadScore { get; set; }
        public int ActiveChats { get; set; }
        public int RankingOrder { get; set; }
        public string Status { get; set; } = "Eligible";
        public string? Reason { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Escalation Escalation { get; set; } = null!;
    }
}
