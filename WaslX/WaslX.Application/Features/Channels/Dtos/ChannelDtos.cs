using System.Collections.Generic;

namespace WaslX.Application.Features.Channels.Dtos;

/// <summary>A WhatsApp number that belongs to a channel (light summary for the channel screens).</summary>
public record ChannelWhatsAppSummary(int WhatsAppAccountId, string PhoneNumber, string PlatformName);

/// <summary>A channel: a tenant-defined grouping of WhatsApp numbers that agents are granted access to.</summary>
public record ChannelResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<ChannelWhatsAppSummary> WhatsAppAccounts,
    int AgentCount);

/// <summary>Create/update payload. WhatsAppAccountIds are the numbers (owned by the tenant) placed in the channel.</summary>
public record UpsertChannelRequest(
    string Name,
    string? Description,
    IReadOnlyList<int>? WhatsAppAccountIds);
