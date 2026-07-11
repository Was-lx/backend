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

/// <summary>Cursor-paginated slice of items plus a flag indicating more are available.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, bool HasMore);
