using System.Collections.Generic;

namespace WaslX.Application.Features.Groups.Dtos;

/// <summary>A stage inside a group (an ordered step a conversation moves through, e.g. New → Qualified → Won).</summary>
public record StageResponse(int Id, string Name, int SequenceOrder);

/// <summary>A group / team: a tenant-defined set of agents with its own ordered stages for cross-team handoff.</summary>
public record GroupResponse(
    int Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsAssignableByDistribution,
    IReadOnlyList<StageResponse> Stages,
    int MemberCount);

/// <summary>Create/update payload for a group.</summary>
public record UpsertGroupRequest(string Name, string? Description, bool IsAssignableByDistribution);

/// <summary>Create/update payload for a single stage (order is managed by append / reorder).</summary>
public record UpsertStageRequest(string Name);

/// <summary>Reorder payload: the stage ids in the desired order (SequenceOrder is rewritten to match).</summary>
public record ReorderStagesRequest(IReadOnlyList<int> StageIdsInOrder);

/// <summary>Add-member payload. UserId is the ApplicationUser id (string) as returned by the users API.</summary>
public record AddGroupMemberRequest(string UserId);
