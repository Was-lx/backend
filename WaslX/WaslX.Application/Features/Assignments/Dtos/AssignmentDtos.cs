using System;

namespace WaslX.Application.Features.Assignments.Dtos;

/// <summary>Assign/reassign payload. TargetUserId is the Identity (ApplicationUser) id of the agent to assign to.</summary>
public record AssignRequest(string TargetUserId, string? Reason);

/// <summary>One row of a conversation's assignment history (newest first on the wire).</summary>
public record AssignmentHistoryItem(
    int Id,
    string AssignedToName,
    string? AssignedByName,
    string Method,
    string? Reason,
    DateTime AssignedAt);

/// <summary>Light summary of an unassigned conversation for the "Unassigned" queue.</summary>
public record UnassignedConversationItem(
    int ConversationId,
    string CustomerName,
    string CustomerPhone,
    DateTime? LastMessageAt,
    int WhatsAppAccountId);
