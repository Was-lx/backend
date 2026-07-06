using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Abstractions.Profile;
using WaslX.Application.Features.Profile.Dtos;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class ProfileService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService) : IProfileService
{
    public async Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure<MeResponse>(UserErrors.UserNotFound);

        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Agent";

        var tenant = user.TenantId is null
            ? null
            : await db.Tenants.AsNoTracking()
                .Where(t => t.Id == user.TenantId)
                .Select(t => new { t.Name, t.OnboardingCompleted, t.OnboardingStep })
                .FirstOrDefaultAsync(cancellationToken);

        var effective = await permissionService.GetEffectiveAsync(user.TenantId, primaryRole, cancellationToken);

        return Result.Success(new MeResponse(
            user.Id, user.Email!, user.FullName, user.PhoneNumber,
            roles.ToList(), user.TenantId, tenant?.Name,
            effective.Granted, effective.Scopes,
            tenant?.OnboardingCompleted ?? true, tenant?.OnboardingStep ?? 0));
    }
}
