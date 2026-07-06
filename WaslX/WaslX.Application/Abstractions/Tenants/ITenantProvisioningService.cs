using WaslX.Application.Features.Auth.Dtos;
using WaslX.Application.Features.Tenants.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Tenants;

public interface ITenantProvisioningService
{
    /// <summary>
    /// Self-serve sign-up. Creates the workspace on a 7-day trial, its owner Admin user
    /// (auto-verified), seeds the default permission matrix, and returns a live session so
    /// the new Admin lands straight in onboarding.
    /// </summary>
    Task<Result<AuthResponse>> CreateSelfServeAsync(SelfServeSignupInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// SuperAdmin provisions a workspace + its first Admin (who receives a set-password email),
    /// on the chosen plan, optionally starting a trial. Seeds the default permission matrix.
    /// </summary>
    Task<Result<int>> CreateBySuperAdminAsync(SuperAdminCreateTenantInput input, CancellationToken cancellationToken = default);
}
