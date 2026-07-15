using System;
using System.Collections.Generic;

namespace WaslX.Application.Features.ConversationStages.Dtos;

/// <summary>Route payload: the group / team the conversation should enter (its first stage is picked automatically).</summary>
public record RouteToGroupRequest(int GroupId);

/// <summary>Move payload: the stage (within the conversation's CURRENT group) to move the conversation into.</summary>
public record MoveToStageRequest(int StageId);

/// <summary>Cross-team handoff payload: the group / team the conversation is handed off to.</summary>
public record HandoffRequest(int TargetGroupId);

/// <summary>One conversation card on a stage board column (light projection for the pipeline view).</summary>
public record BoardConversationItem(
    int Id,
    string CustomerName,
    string CustomerPhone,
    string Status,
    string? AssignedUserName,
    DateTime? LastMessageAt);

/// <summary>
/// One column of a group's stage board: a stage and the conversations currently sitting in it.
/// StageId is null for the synthetic "Unstaged" column (conversations routed to the group but not yet
/// in any stage — e.g. a group with no stages, or freshly handed-off).
/// </summary>
public record StageBoardColumn(
    int? StageId,
    string StageName,
    int SequenceOrder,
    IReadOnlyList<BoardConversationItem> Conversations);
