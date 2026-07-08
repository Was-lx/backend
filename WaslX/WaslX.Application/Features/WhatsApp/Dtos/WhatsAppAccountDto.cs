namespace WaslX.Application.Features.WhatsApp.Dtos;

/// <summary>Safe view of a tenant's WhatsApp connection — never exposes the access token.</summary>
public record WhatsAppAccountDto(
    int Id,
    string PhoneNumber,
    string PhoneNumberId,
    string WhatsAppBusinessAccountId,
    string Status,
    DateTime ConnectedAt,
    DateTime? TokenExpiresAt);

/// <summary>Outcome of an outbound send: the persisted message plus Meta's message id.</summary>
public record SendMessageResult(
    int MessageId,
    int ConversationId,
    string WhatsAppMessageId,
    string Status);
