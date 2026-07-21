namespace WaslX.Application.Features.Escalation.Models;

public sealed class EscalationInput
{
    public int TenantId { get; init; }
    public int ConversationId { get; init; }
    public int MessageId { get; init; }
    public int ClassificationId { get; init; }
    public string Topic { get; init; } = "general";
    public string Sentiment { get; init; } = "neutral";
    public string Priority { get; init; } = "normal";
    public string Reason { get; init; } = string.Empty;
}

public sealed class EscalationResult
{
    public int EscalationId { get; init; }
    public int ConversationId { get; init; }
    public bool Created { get; init; }
    public bool AlreadyEscalated { get; init; }
    public string Status { get; init; } = "open";
}


