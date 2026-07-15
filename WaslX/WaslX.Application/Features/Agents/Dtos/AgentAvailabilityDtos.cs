using System;

namespace WaslX.Application.Features.Agents.Dtos;

/// <summary>The calling agent's live presence: whether they're connected, on break, and last seen.</summary>
public record AvailabilityResponse(bool IsOnline, bool IsOnBreak, DateTime? LastSeenAt);

/// <summary>Toggle payload for the agent break state.</summary>
public record SetBreakRequest(bool OnBreak);
