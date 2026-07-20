namespace WaslX.Application.Features.AiAgent.Dtos;

public record AiAgentNumberSettingsDto(int WhatsAppAccountId, bool Enabled, bool AutoReplyEnabled, bool AllowOutsideWindow, int MaxConversationMessages);

public record AiAgentSettingsResponse(
    bool Enabled,
    List<AiAgentNumberSettingsDto> PerNumber,
    string PersonaName,
    string ToneInstructions,
    decimal HandoffThreshold);

public record UpdateAiAgentSettingsRequest(
    bool Enabled,
    List<AiAgentNumberSettingsDto> PerNumber,
    string PersonaName,
    string ToneInstructions,
    decimal HandoffThreshold);

public record AiKnowledgeFileDto(
    int Id,
    string FileName,
    long SizeBytes,
    DateTime UploadedAt);

public record AiHandledConversationDto(
    int ConversationId,
    string CustomerName,
    string CustomerPhone,
    string Status,
    DateTime? LastMessageAt);
