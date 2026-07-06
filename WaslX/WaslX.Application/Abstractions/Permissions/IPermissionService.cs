using WaslX.Application.Features.Permissions.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Permissions;

public interface IPermissionService
{
    /// <summary>Resolve the effective permissions for a role in a tenant (defaults + tenant toggles + hard locks).</summary>
    Task<EffectivePermissionsResponse> GetEffectiveAsync(int? tenantId, string role, CancellationToken cancellationToken = default);

    /// <summary>True if the role in the tenant may perform the permission.</summary>
    Task<bool> HasAsync(int? tenantId, string role, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>The full editable Roles × Permissions matrix for the tenant's settings screen.</summary>
    Task<Result<PermissionMatrixResponse>> GetTenantMatrixAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Apply toggle changes from the settings screen. Locked cells and Admin are rejected.</summary>
    Task<Result> UpdateTenantMatrixAsync(int tenantId, IReadOnlyList<PermissionUpdateItem> changes, CancellationToken cancellationToken = default);

    /// <summary>Seed the default matrix into a freshly-created tenant.</summary>
    Task SeedDefaultMatrixAsync(int tenantId, CancellationToken cancellationToken = default);
}
