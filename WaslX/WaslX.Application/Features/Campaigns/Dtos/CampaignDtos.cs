namespace WaslX.Application.Features.Campaigns.Dtos;

/// <summary>List/summary view of an outbound WhatsApp broadcast campaign.</summary>
public record CampaignResponse(
    int Id,
    string Name,
    string Status,
    string TemplateName,
    int AudienceCount,
    int SentCount,
    int DeliveredCount,
    int ReadCount,
    int FailedCount,
    DateTime? ScheduledAt,
    DateTime CreatedAt);

/// <summary>Full campaign view including the message body and the raw audience filter.</summary>
public record CampaignDetailResponse(
    int Id,
    string Name,
    string Status,
    string TemplateName,
    string MessageBody,
    string AudienceFilter,
    int AudienceCount,
    int SentCount,
    int DeliveredCount,
    int ReadCount,
    int FailedCount,
    DateTime? ScheduledAt,
    DateTime CreatedAt);

/// <summary>Create payload for a campaign. <paramref name="ScheduledAt"/> null = send on launch.</summary>
public record CreateCampaignRequest(
    string Name,
    string TemplateName,
    string MessageBody,
    string? AudienceFilter,
    DateTime? ScheduledAt);

/// <summary>Update payload for a Draft campaign.</summary>
public record UpdateCampaignRequest(
    string Name,
    string TemplateName,
    string MessageBody,
    string? AudienceFilter,
    DateTime? ScheduledAt);

/// <summary>Preview how many customers an audience filter resolves to (before launch).</summary>
public record AudiencePreviewRequest(string? AudienceFilter);

/// <summary>Preview result: the resolved count plus a small sample of the matched customers.</summary>
public record AudiencePreviewResponse(int Count, IReadOnlyList<AudienceSampleItem> Sample);

/// <summary>A single customer in an audience preview sample.</summary>
public record AudienceSampleItem(int CustomerId, string Name, string PhoneNumber);

/// <summary>A selectable existing contact for the "pick from my contacts" campaign audience mode.</summary>
public record AudienceContactDto(int Id, string Name, string Phone, DateTime? LastContactAt);

/// <summary>Per-recipient delivery row for a campaign.</summary>
public record CampaignRecipientResponse(
    int Id,
    int CustomerId,
    string CustomerName,
    string PhoneNumber,
    string Status,
    string? Error,
    DateTime? SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt);
