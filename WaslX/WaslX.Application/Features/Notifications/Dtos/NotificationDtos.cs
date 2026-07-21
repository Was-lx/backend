namespace WaslX.Application.Features.Notifications.Dtos;

/// <summary>A single in-app notification for the current user.</summary>
public record NotificationResponse(
    int Id,
    string Type,
    string Title,
    string Body,
    string? EntityType,
    int? EntityId,
    bool IsRead,
    DateTime CreatedAt);
