using System.Collections.Generic;

namespace WaslX.Application.Features.AgentAccess.Dtos;

/// <summary>An agent's full access &amp; distribution assignment: channels, distribution numbers, groups and shifts.</summary>
public record AgentAccessResponse(
    string UserId,
    IReadOnlyList<int> ChannelIds,
    IReadOnlyList<int> DistributionWhatsAppAccountIds,
    IReadOnlyList<int> GroupIds,
    IReadOnlyList<int> ShiftIds);

/// <summary>
/// Set payload. Each list is reconciled (add missing / remove extra). Only ids owned by the tenant are kept;
/// a distribution WhatsApp number is dropped if its channel is not among the agent's channels.
/// </summary>
public record SetAgentAccessRequest(
    IReadOnlyList<int>? ChannelIds,
    IReadOnlyList<int>? DistributionWhatsAppAccountIds,
    IReadOnlyList<int>? GroupIds,
    IReadOnlyList<int>? ShiftIds);
