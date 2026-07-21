using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Feature-flag management + resolution (US-6.7 — Platform Owner console). A null-tenant row is the global
/// default for a key; per-tenant rows override it. Cross-tenant — only reached through the SuperAdmin
/// console (<c>[Authorize(Roles = "SuperAdmin")]</c>). Every mutation is written to the platform audit trail.
/// </summary>
public interface IFeatureFlagService
{
    Task<Result<IReadOnlyList<FeatureFlagResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<FeatureFlagResponse>> UpsertAsync(UpsertFeatureFlagInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<FeatureFlagResponse>> ToggleAsync(int id, ToggleFeatureFlagInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default);

    /// <summary>Resolve a key's effective value for a tenant: a per-tenant row wins over the global row.</summary>
    Task<Result<ResolvedFeatureFlagResponse>> ResolveAsync(string key, int? tenantId, CancellationToken cancellationToken = default);
}
