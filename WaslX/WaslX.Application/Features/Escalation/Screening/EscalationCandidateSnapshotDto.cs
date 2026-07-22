namespace WaslX.Application.Features.Escalation.Screening
{
    public sealed record EscalationCandidateSnapshotDto(
        int AgentId,
        string AgentName,
        decimal OverallScore,
        decimal PerformanceScore,
        decimal ResponseSpeedScore,
        decimal WorkloadScore,
        int ActiveChats,
        int RankingOrder,
        string Status,
        string? Reason);
}
