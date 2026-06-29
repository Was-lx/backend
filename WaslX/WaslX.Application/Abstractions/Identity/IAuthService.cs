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

    // Email confirmation (OTP based)
    Task<Result<AuthResponse>> ConfirmEmailAsync(string email, string otp);
    Task<Result> ResendConfirmationEmailAsync(string email);

    // Password reset (emailed code, ASP.NET Identity reset token)
    Task<Result> SendResetPasswordCodeAsync(string email);
    Task<Result> ResetPasswordAsync(string email, string code, string newPassword);

    // Authenticated user
    Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    Task<Result<IReadOnlyList<string>>> GetRolesAsync(string userId);

    // Admin user & role management
    Task<Result<string>> CreateUserAsync(string email, string fullName, string role, int? tenantId, string? phoneNumber, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(string userId, string role);
    Task<Result> SetUserStatusAsync(string userId, bool isDisabled);
    Task<Result<IReadOnlyList<UserResponse>>> GetUsersAsync(int? tenantId, CancellationToken cancellationToken = default);

    // Roles
    Task<Result<IReadOnlyList<RoleResponse>>> GetRolesListAsync();
}
