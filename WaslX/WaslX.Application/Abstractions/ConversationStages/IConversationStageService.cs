using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.ConversationStages.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.ConversationStages;

/// <summary>
/// Conversation stage pipeline + cross-team handoff (tenant-scoped). Route moves a conversation into a
/// group and its first stage; MoveToStage advances it within its current group's stages; Handoff transfers
/// it to another team (clearing the assignment so the receiving team picks it up). Every mutation writes a
/// <c>ConversationStageHistory</c> row and pushes a realtime conversation-changed notification.
/// </summary>
public interface IConversationStageService
{
    /// <summary>Routes a conversation into <paramref name="groupId"/> and its first stage (lowest SequenceOrder, may be none).</summary>
    Task<Result> RouteToGroupAsync(int? tenantId, int conversationId, int groupId, int byUserId, string? byUserEmail, bool isPrivileged, CancellationToken cancellationToken = default);

    /// <summary>Moves a conversation to <paramref name="stageId"/>, which must belong to its current group.</summary>
    Task<Result> MoveToStageAsync(int? tenantId, int conversationId, int stageId, int byUserId, string? byUserEmail, bool isPrivileged, CancellationToken cancellationToken = default);

    /// <summary>Cross-team handoff: moves the conversation to <paramref name="targetGroupId"/>'s first stage and clears the assignment.</summary>
    Task<Result> HandoffAsync(int? tenantId, int conversationId, int targetGroupId, int byUserId, string? byUserEmail, bool isPrivileged, CancellationToken cancellationToken = default);

    /// <summary>Returns the group's stage board (columns of conversations), tenant- and role-scoped (agents see only their own).</summary>
    Task<Result<IReadOnlyList<StageBoardColumn>>> GetBoardAsync(int? tenantId, int groupId, bool isPrivileged, int currentUserId, CancellationToken cancellationToken = default);
}
