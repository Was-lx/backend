using WaslX.Application.Features.Platform.Dtos;
using WaslX.Application.Features.Tenants.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Tenants;

public interface ITenantService
{
    Task<Result<TenantProfileResponse>> GetProfileAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result> UpdateProfileAsync(int tenantId, UpdateTenantProfileInput input, CancellationToken cancellationToken = default);
    Task<Result> UpdateOnboardingAsync(int tenantId, int step, bool completed, CancellationToken cancellationToken = default);

    // SuperAdmin console
    Task<Result<IReadOnlyList<TenantSummaryResponse>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Full tenant detail (plan, counts, usage limits) for the console detail view (US-6.2).</summary>
    Task<Result<TenantDetailResponse>> GetDetailAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Configure a tenant — its profile plus which plan it is on (US-6.2). Audited.</summary>
    Task<Result> ConfigureAsync(int tenantId, ConfigureTenantInput input, PlatformActor actor, CancellationToken cancellationToken = default);

    /// <summary>Set the tenant lifecycle status (Active | Suspended | Pending | Cancelled). Audited.</summary>
    Task<Result> SetStatusAsync(int tenantId, string status, PlatformActor actor, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete / deactivate a tenant (marks it Cancelled; data is retained). Audited (US-6.2).</summary>
    Task<Result> SoftDeleteAsync(int tenantId, PlatformActor actor, CancellationToken cancellationToken = default);
}
