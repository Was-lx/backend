namespace WaslX.Application.Features.Escalation.Models
{
    public sealed class EscalationScoringOptions
    {
        public const string SectionName = "EscalationScoring";

        public decimal PerformanceWeight { get; set; } = 0.5m;
        public decimal ResponseSpeedWeight { get; set; } = 0.25m;
        public decimal WorkloadWeight { get; set; } = 0.25m;
        public int WorkloadTolerance { get; set; } = 2;
        public int AvgResponseTimeTargetSeconds { get; set; } = 120;
    }
}
