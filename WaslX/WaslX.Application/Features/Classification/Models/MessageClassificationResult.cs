namespace WaslX.Application.Features.Classification.Models;

public sealed class MessageClassificationResult
{
    public string Topic { get; init; } = "general";
    public string Language { get; init; } = "unknown";
    public string Sentiment { get; init; } = "neutral";
    public string Priority { get; init; } = "normal";
    public bool Escalate { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ClassifierVersion { get; init; } = "unknown";
}
