namespace WaslX.Application.Features.Classification.Models;

public sealed class MessageClassificationInput
{
    public int TenantId { get; init; }
    public int ConversationId { get; init; }
    public int MessageId { get; init; }
    public string MessageText { get; init; } = string.Empty;
    public IReadOnlyList<string> RecentMessages { get; init; } = Array.Empty<string>();
    public string? CustomerMemorySummary { get; init; }
}
