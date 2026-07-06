using WaslX.Application.Features.Auth.Dtos;
using WaslX.Application.Features.Roles.Dtos;
using WaslX.Application.Features.Users.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Identity;

public interface IAuthService
{
    // Public auth flows
    Task<Result> RegisterAsync(string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default);

    // Email confirmation (OTP based)
    Task<Result<AuthResponse>> ConfirmEmailAsync(string email, string otp);
    Task<Result> ResendConfirmationEmailAsync(string email);

    // Password reset (emailed code, ASP.NET Identity reset token)
    Task<Result> SendResetPasswordCodeAsync(string email);
    Task<Result> ResetPasswordAsync(string email, string code, string newPassword);

    // Authenticated user
    Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    Task<Result<IReadOnlyList<string>>> GetRolesAsync(string userId);

    // Issue a fresh access + refresh token pair for an already-created user
    // (used right after self-serve sign-up to log the new Admin straight in).
    Task<Result<AuthResponse>> BuildSessionAsync(string userId);

    // Admin user & role management.
    // callerTenantId scopes the mutation to the caller's tenant (null = SuperAdmin, may act cross-tenant).
    Task<Result<string>> CreateUserAsync(string email, string fullName, string role, int? tenantId, string? phoneNumber, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(string userId, string fullName, string? phoneNumber, int? callerTenantId, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(string userId, string role, int? callerTenantId);
    Task<Result> SetUserStatusAsync(string userId, bool isDisabled, int? callerTenantId);
    Task<Result<IReadOnlyList<UserResponse>>> GetUsersAsync(int? tenantId, CancellationToken cancellationToken = default);

    // Roles
    Task<Result<IReadOnlyList<RoleResponse>>> GetRolesListAsync();
}
