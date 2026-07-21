using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Platform-owner management of SuperAdmin users (US-6.1). Operates on Identity users that carry the
/// SuperAdmin role and no tenant. Cross-tenant by nature — only ever reached through the SuperAdmin
/// console (<c>[Authorize(Roles = "SuperAdmin")]</c>). Every mutation is written to the platform audit trail.
/// </summary>
public interface ISuperAdminUserService
{
    Task<Result<IReadOnlyList<SuperAdminUserResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<SuperAdminUserResponse>> CreateAsync(CreateSuperAdminInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result> SetStatusAsync(string userId, bool isDisabled, PlatformActor actor, CancellationToken cancellationToken = default);
}
