namespace WaslX.Application.Features.Escalation.Models
{
    public sealed class AgentPerformanceSnapshot
    {
        public int UserId { get; init; }
        public decimal PerformanceScore { get; init; } = 0.5m;
        public decimal ResponseSpeedScore { get; init; } = 0.5m;
        public decimal ResolutionScore { get; init; } = 0.5m;
    }
}
