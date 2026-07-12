namespace WaslX.Domain.SharedEnums;

/// <summary>
/// The conversation lifecycle state machine (US-2.8). Only three transitions are automatic
/// (new inbound → New, first agent reply → InProgress, inbound on Resolved → Reopened, handled in
/// the send/webhook paths); every transition below is the set of valid MANUAL moves an agent/manager
/// may make from a given status.
/// </summary>
public static class ConversationStatusTransitions
{
    private static readonly IReadOnlyDictionary<ConversationStatus, ConversationStatus[]> Map =
        new Dictionary<ConversationStatus, ConversationStatus[]>
        {
            [ConversationStatus.New] = [ConversationStatus.Assigned, ConversationStatus.InProgress, ConversationStatus.Pending, ConversationStatus.Resolved],
            [ConversationStatus.Assigned] = [ConversationStatus.InProgress, ConversationStatus.Pending, ConversationStatus.Resolved],
            [ConversationStatus.InProgress] = [ConversationStatus.Assigned, ConversationStatus.Pending, ConversationStatus.Resolved],
            [ConversationStatus.Pending] = [ConversationStatus.Assigned, ConversationStatus.InProgress, ConversationStatus.Resolved],
            [ConversationStatus.Resolved] = [ConversationStatus.Reopened],
            [ConversationStatus.Reopened] = [ConversationStatus.Assigned, ConversationStatus.InProgress, ConversationStatus.Pending, ConversationStatus.Resolved],
        };

    /// <summary>The statuses a conversation may be moved to manually from <paramref name="from"/>.</summary>
    public static IReadOnlyList<ConversationStatus> AllowedNext(ConversationStatus from) =>
        Map.TryGetValue(from, out var next) ? next : [];

    /// <summary>True when <paramref name="to"/> is a valid manual transition from <paramref name="from"/>.</summary>
    public static bool CanTransition(ConversationStatus from, ConversationStatus to) =>
        AllowedNext(from).Contains(to);
}
