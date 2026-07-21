using Microsoft.AspNetCore.Identity;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Identity;

namespace WaslX.Persistance.Services;

/// <summary>
/// Platform-owner management of SuperAdmin users (US-6.1). SuperAdmins are Identity users with the
/// SuperAdmin role and <c>TenantId = null</c>; there is no dedicated entity. Reuses the existing
/// <see cref="IAuthService.CreateUserAsync"/> account-creation flow (random temp password + emailed
/// set-password code) rather than duplicating it. Cross-tenant — guarded by the SuperAdmin controller.
/// Every mutation is written to the global platform audit trail.
/// </summary>
internal sealed class SuperAdminUserService(
    UserManager<ApplicationUser> userManager,
    IAuthService authService,
    IPlatformAuditService audit) : ISuperAdminUserService
{
    public async Task<Result<IReadOnlyList<SuperAdminUserResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.GetUsersInRoleAsync(DefaultRoles.SuperAdmin);
        var list = users
            .OrderBy(u => u.Email)
            .Select(Map)
            .ToList();
        return Result.Success<IReadOnlyList<SuperAdminUserResponse>>(list);
    }

    public async Task<Result<SuperAdminUserResponse>> CreateAsync(CreateSuperAdminInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var email = input.Email.Trim().ToLowerInvariant();

        // Reuse the admin-user creation path: dup-email + role checks, random temp password,
        // EmailConfirmed = true, role assigned, and an emailed set-password code.
        var created = await authService.CreateUserAsync(email, input.FullName.Trim(), DefaultRoles.SuperAdmin, tenantId: null, Clean(input.Phone), cancellationToken);
        if (created.IsFailure)
            return Result.Failure<SuperAdminUserResponse>(created.Error);

        var user = await userManager.FindByIdAsync(created.Value);
        if (user is null)
            return Result.Failure<SuperAdminUserResponse>(UserErrors.UserNotFound);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "SuperAdminCreated", "PlatformUser", user.Id,
            null, $"email='{email}'", actor.Ip, cancellationToken);

        return Result.Success(Map(user));
    }

    public async Task<Result> SetStatusAsync(string userId, bool isDisabled, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        // Only manage users that are actually platform SuperAdmins through this surface.
        if (!await userManager.IsInRoleAsync(user, DefaultRoles.SuperAdmin))
            return Result.Failure(AppErrors.NotASuperAdmin);

        user.IsDisabled = isDisabled;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var e = result.Errors.First();
            return Result.Failure(new Error(e.Code, e.Description, 400));
        }

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail,
            isDisabled ? "SuperAdminDisabled" : "SuperAdminEnabled", "PlatformUser", user.Id,
            null, $"email='{user.Email}'", actor.Ip, cancellationToken);

        return Result.Success();
    }

    private static SuperAdminUserResponse Map(ApplicationUser u) =>
        new(u.Id, u.Email ?? string.Empty, u.FullName, u.PhoneNumber, u.IsDisabled, u.EmailConfirmed);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
