using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.Assignments.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Assignments;

/// <summary>
/// Manual assignment / reassignment (tenant-scoped): sets the conversation owner, records an
/// Assignment history row, exposes the per-conversation history and the tenant's Unassigned queue.
/// </summary>
public interface IAssignmentService
{
    Task<Result> AssignAsync(int? tenantId, int conversationId, string targetAppUserId, string byAppUserId, string? reason, CancellationToken cancellationToken = default);
    Task<Result> ReassignAsync(int? tenantId, int conversationId, string targetAppUserId, string byAppUserId, string? reason, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AssignmentHistoryItem>>> GetHistoryAsync(int? tenantId, int conversationId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UnassignedConversationItem>>> GetUnassignedAsync(int? tenantId, CancellationToken cancellationToken = default);
}
