using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.Groups.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Groups;

/// <summary>Groups / teams, their stages and their membership (tenant-scoped).</summary>
public interface IGroupService
{
    Task<Result<IReadOnlyList<GroupResponse>>> GetAllAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> GetByIdAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> CreateAsync(int? tenantId, UpsertGroupRequest request, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> UpdateAsync(int? tenantId, int id, UpsertGroupRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int? tenantId, int id, CancellationToken cancellationToken = default);

    // Stages
    Task<Result<GroupResponse>> CreateStageAsync(int? tenantId, int groupId, UpsertStageRequest request, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> UpdateStageAsync(int? tenantId, int groupId, int stageId, UpsertStageRequest request, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> DeleteStageAsync(int? tenantId, int groupId, int stageId, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> ReorderStagesAsync(int? tenantId, int groupId, ReorderStagesRequest request, CancellationToken cancellationToken = default);

    // Membership
    Task<Result<GroupResponse>> AddMemberAsync(int? tenantId, int groupId, string userId, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> RemoveMemberAsync(int? tenantId, int groupId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Creates a default "Sales" group (with one "New" stage) if the tenant has no default group yet.</summary>
    Task<Result<GroupResponse>> EnsureDefaultSalesGroupAsync(int? tenantId, CancellationToken cancellationToken = default);
}
