using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Global platform secret / credential management (US-6.6 — Platform Owner console). Values are encrypted
/// at rest with ASP.NET Core Data Protection and are NEVER returned to any caller (only a masked preview
/// is exposed); these secrets are never reachable from tenant APIs. Global — no tenant scope, guarded by
/// the SuperAdmin console. Every mutation is written to the platform audit trail.
/// </summary>
public interface IPlatformCredentialService
{
    Task<Result<IReadOnlyList<PlatformCredentialResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PlatformCredentialResponse>> CreateAsync(CreatePlatformCredentialInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<PlatformCredentialResponse>> UpdateAsync(int id, UpdatePlatformCredentialInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<PlatformCredentialResponse>> RotateAsync(int id, RotatePlatformCredentialInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default);
}
