namespace WaslX.Application.Features.Escalation.Models
{
    public sealed class WorkloadSnapshot
    {
        public int UserId { get; init; }
        public int OpenEscalations { get; init; }
        public int ActiveConversations { get; init; }
    }
}
