namespace WaslX.Application.Features.Conversations.Dtos;

/// <summary>One row in the shared-inbox conversation list.</summary>
public record ConversationListItemResponse(
    int Id,
    string CustomerName,
    string CustomerPhone,
    string Status,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    int? AssignedUserId,
    int UnreadCount);

/// <summary>A single message in a conversation thread (safe projection — no tokens).</summary>
public record MessageResponse(
    int Id,
    string SenderType,
    string Content,
    string MessageType,
    string Status,
    DateTime Timestamp,
    int? SenderUserId,
    string? MediaUrl,
    string? MediaMimeType,
    string? MediaFileName);

/// <summary>Rich detail for the customer-context panel + status controls.</summary>
public record ConversationDetailResponse(
    int Id,
    string CustomerName,
    string CustomerPhone,
    bool CustomerVip,
    string Status,
    IReadOnlyList<string> AllowedTransitions,
    int? AssignedUserId,
    string? AssignedUserName,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    DateTime? LastInboundAt,
    DateTime? WindowExpiresAt,
    bool IsWindowOpen,
    string WindowType,
    long RemainingSeconds,
    int MessageCount,
    int? GroupId,
    string? GroupName,
    int? CurrentStageId,
    string? CurrentStageName);

/// <summary>Returned after a status change: the new status plus the moves now valid from it.</summary>
public record ConversationStatusResponse(int Id, string Status, IReadOnlyList<string> AllowedTransitions);

/// <summary>
/// AI conversation summary. <paramref name="ShortSummary"/> is always present; the structured
/// <paramref name="FullSummary"/> is only populated once explicitly generated. <paramref name="IsStale"/>
/// indicates newer messages have arrived since <paramref name="GeneratedAt"/> (the caller may refresh).
/// </summary>
public record ConversationSummaryResponse(
    int ConversationId,
    string ShortSummary,
    string? FullSummary,
    int MessageCount,
    bool IsStale,
    DateTime GeneratedAt);

/// <summary>Cursor-paginated slice of items plus a flag indicating more are available.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, bool HasMore);

/// <summary>
/// Optional server-side filters for the shared-inbox list (US-3.9). All members are optional;
/// an all-null filter (or null filter) leaves the default inbox behavior unchanged. Filters are
/// applied on top of the existing tenant- and role-scoping — they can only narrow the caller's
/// visible set, never widen it.
/// </summary>
public record ConversationFilter(
    string? Status = null,
    // The caller passes the Identity (ApplicationUser GUID) id here; the service resolves it to the
    // numeric domain user id that Conversation.AssignedUserId actually stores.
    string? AssignedUserId = null,
    bool? Unassigned = null,
    int? GroupId = null,
    int? TagId = null,
    int? WhatsAppAccountId = null,
    int? CustomerId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null);
