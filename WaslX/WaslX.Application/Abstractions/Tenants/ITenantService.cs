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
    Task<Result> SetStatusAsync(int tenantId, string status, CancellationToken cancellationToken = default);
}
