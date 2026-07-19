namespace WaslX.Application.Features.Escalation.Screening
{
    public sealed record OverrideEscalationRequest(int AssigneeId, string Reason);
}
