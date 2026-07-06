using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.Authentication;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Application.Features.Roles.Dtos;
using WaslX.Application.Features.Users.Dtos;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Email;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Identity;

internal class AuthSerive(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IJwtTokenGenerator jwtTokenGenerator,
    IEmailSender emailSender,
    IOptions<AppSettings> appSettings,
    ILogger<AuthSerive> logger) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly IJwtTokenGenerator _jwt = jwtTokenGenerator;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly ILogger<AuthSerive> _logger = logger;

    // In-memory OTP store for email confirmation, keyed by user id.
    private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();
    private const int OtpExpiryMinutes = 15;

    // ---------------------------------------------------------------- Register

    public async Task<Result> RegisterAsync(string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        if (await _userManager.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Result.Failure(UserErrors.DuplicatedEmail);

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        // Self-registered users get the default Agent role until an admin reassigns them.
        await _userManager.AddToRoleAsync(user, DefaultRoles.Agent);

        var otp = GenerateOtp();
        StoreOtp(EmailConfirmKey(user.Id), otp);
        SendConfirmationEmail(user, otp);

        return Result.Success();
    }

    // ------------------------------------------------------------------- Login

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByEmailAsync(email) is not { } user)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (user.IsDisabled)
            return Result.Failure<AuthResponse>(UserErrors.DisabledUser);

        if (await _userManager.IsLockedOutAsync(user))
            return Result.Failure<AuthResponse>(UserErrors.LockedUser);

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.EmailNotConfirmed);

        await _userManager.ResetAccessFailedCountAsync(user);

        var response = await BuildAuthResponseAsync(user);
        return Result.Success(response);
    }

    // ----------------------------------------------------------- Refresh token

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
    {
        var userId = _jwt.ValidateTokenAndGetUserId(token);
        if (userId is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        if (user.IsDisabled)
            return Result.Failure<AuthResponse>(UserErrors.DisabledUser);

        var existing = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken && x.IsActive);
        if (existing is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

        existing.RevokedOn = DateTime.UtcNow;

        var response = await BuildAuthResponseAsync(user);
        return Result.Success(response);
    }

    public async Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
    {
        var userId = _jwt.ValidateTokenAndGetUserId(token);
        if (userId is null)
            return Result.Failure(UserErrors.InvalidJwtToken);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.InvalidJwtToken);

        var existing = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken && x.IsActive);
        if (existing is null)
            return Result.Failure(UserErrors.InvalidRefreshToken);

        existing.RevokedOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        // Revoke every active refresh token so the session can't be renewed.
        var now = DateTime.UtcNow;
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            token.RevokedOn = now;

        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    // ------------------------------------------------------- Email confirmation

    public async Task<Result<AuthResponse>> ConfirmEmailAsync(string email, string otp)
    {
        if (await _userManager.FindByEmailAsync(email) is not { } user)
            return Result.Failure<AuthResponse>(UserErrors.InvalidOtp);

        if (user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.DuplicatedConfirmation);

        if (!TryValidateOtp(EmailConfirmKey(user.Id), otp))
            return Result.Failure<AuthResponse>(UserErrors.InvalidOtp);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure<AuthResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        var response = await BuildAuthResponseAsync(user);
        return Result.Success(response);
    }

    public async Task<Result> ResendConfirmationEmailAsync(string email)
    {
        if (await _userManager.FindByEmailAsync(email) is not { } user)
            return Result.Success();

        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.DuplicatedConfirmation);

        var otp = GenerateOtp();
        StoreOtp(EmailConfirmKey(user.Id), otp);
        SendConfirmationEmail(user, otp);

        return Result.Success();
    }

    // ----------------------------------------------------------- Password reset
    // Uses an emailed code (ASP.NET Identity reset token, Base64URL-encoded).

    public async Task<Result> SendResetPasswordCodeAsync(string email)
    {
        // Don't reveal whether the email exists.
        if (await _userManager.FindByEmailAsync(email) is not { } user)
            return Result.Success();

        if (!user.EmailConfirmed)
            return Result.Failure(UserErrors.EmailNotConfirmed);

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = Base64UrlEncode(code);

        _logger.LogInformation("Password reset code for {email}: {code}", user.Email, code);
        SendResetPasswordEmail(user, code);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string email, string code, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.EmailConfirmed)
            return Result.Failure(UserErrors.InvalidCode);

        IdentityResult result;
        try
        {
            var decoded = Base64UrlDecode(code);
            result = await _userManager.ResetPasswordAsync(user, decoded, newPassword);
        }
        catch (FormatException)
        {
            result = IdentityResult.Failed(_userManager.ErrorDescriber.InvalidToken());
        }

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();
        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    // --------------------------------------------------------- Change password

    public async Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();
        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    public async Task<Result<IReadOnlyList<string>>> GetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<IReadOnlyList<string>>(UserErrors.UserNotFound);

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success<IReadOnlyList<string>>(roles.ToList());
    }

    public async Task<Result<AuthResponse>> BuildSessionAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var response = await BuildAuthResponseAsync(user);
        return Result.Success(response);
    }

    // ------------------------------------------------ Admin user management

    public async Task<Result<string>> CreateUserAsync(string email, string fullName, string role, int? tenantId, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            return Result.Failure<string>(UserErrors.InvalidRoles);

        if (await _userManager.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Result.Failure<string>(UserErrors.DuplicatedEmail);

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            TenantId = tenantId,
            EmailConfirmed = true,
        };

        // Admin-created accounts get a random password; the user sets their own
        // via the emailed reset code below.
        var tempPassword = GenerateTemporaryPassword();
        var result = await _userManager.CreateAsync(user, tempPassword);

        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure<string>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        await _userManager.AddToRoleAsync(user, role);

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = Base64UrlEncode(code);
        _logger.LogInformation("Set-password code for new user {email}: {code}", user.Email, code);
        SendResetPasswordEmail(user, code);

        return Result.Success(user.Id);
    }

    public async Task<Result> UpdateUserAsync(string userId, string fullName, string? phoneNumber, int? callerTenantId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || IsCrossTenant(user, callerTenantId))
            return Result.Failure(UserErrors.UserNotFound);

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();
        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    public async Task<Result> AssignRoleAsync(string userId, string role, int? callerTenantId)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            return Result.Failure(UserErrors.InvalidRoles);

        // A tenant admin can never grant the platform-level SuperAdmin role.
        if (callerTenantId is not null && string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(UserErrors.InvalidRoles);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || IsCrossTenant(user, callerTenantId))
            return Result.Failure(UserErrors.UserNotFound);

        // Each user has exactly one role: replace any existing roles.
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var result = await _userManager.AddToRoleAsync(user, role);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();
        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    public async Task<Result> SetUserStatusAsync(string userId, bool isDisabled, int? callerTenantId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || IsCrossTenant(user, callerTenantId))
            return Result.Failure(UserErrors.UserNotFound);

        user.IsDisabled = isDisabled;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    /// <summary>
    /// A tenant admin (callerTenantId != null) may only touch users inside their own
    /// tenant. SuperAdmin (callerTenantId == null) is unrestricted. Returning "not found"
    /// for a cross-tenant target avoids leaking that the user exists elsewhere.
    /// </summary>
    private static bool IsCrossTenant(ApplicationUser user, int? callerTenantId) =>
        callerTenantId is not null && user.TenantId != callerTenantId;

    public async Task<Result<IReadOnlyList<UserResponse>>> GetUsersAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (tenantId is not null)
            query = query.Where(u => u.TenantId == tenantId);

        var users = await query.ToListAsync(cancellationToken);

        var responses = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            responses.Add(new UserResponse(
                user.Id,
                user.Email!,
                user.FullName,
                user.PhoneNumber,
                user.TenantId,
                roles.ToList(),
                user.IsDisabled,
                user.EmailConfirmed));
        }

        return Result.Success<IReadOnlyList<UserResponse>>(responses);
    }

    public async Task<Result<IReadOnlyList<RoleResponse>>> GetRolesListAsync()
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => new RoleResponse(r.Id, r.Name!, r.IsDefault))
            .ToListAsync();

        return Result.Success<IReadOnlyList<RoleResponse>>(roles);
    }

    // ----------------------------------------------------------------- Helpers

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _jwt.GenerateAccessToken(user.Id, user.Email!, user.FullName, roles, user.TenantId);
        var refreshToken = _jwt.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken.Token,
            ExpiresOn = refreshToken.ExpiresOn,
        });

        await _userManager.UpdateAsync(user);

        return new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            roles.ToList(),
            accessToken.Token,
            accessToken.ExpiresOn,
            refreshToken.Token,
            user.IsDisabled,
            user.EmailConfirmed);
    }

    private void SendConfirmationEmail(ApplicationUser user, string otp)
    {
        var body = EmailBodyBuilder.GenerateEmailBody("EmailConfirmation", new Dictionary<string, string>
        {
            { "{{name}}", user.FullName },
            { "{{otp_code}}", otp },
        });

        BackgroundJob.Enqueue(() => _emailSender.SendEmailAsync(user.Email!, "✅ WaslX: Email Confirmation", body));
    }

    private void SendResetPasswordEmail(ApplicationUser user, string code)
    {
        var actionUrl = $"{_appSettings.FrontendBaseUrl}/login/reset-password?email={Uri.EscapeDataString(user.Email!)}&code={code}";

        var body = EmailBodyBuilder.GenerateEmailBody("ForgetPassword", new Dictionary<string, string>
        {
            { "{{name}}", user.FullName },
            { "{{code}}", code },
            { "{{action_url}}", actionUrl },
        });

        BackgroundJob.Enqueue(() => _emailSender.SendEmailAsync(user.Email!, "✅ WaslX: Reset your password", body));
    }

    private static string EmailConfirmKey(string userId) => $"EMAIL_CONFIRM_{userId}";

    private static void StoreOtp(string key, string otp) =>
        _otpStore[key] = (otp, DateTime.UtcNow.AddMinutes(OtpExpiryMinutes));

    private static bool TryValidateOtp(string key, string otp)
    {
        if (!_otpStore.TryGetValue(key, out var info) || info.Otp != otp || info.Expiry < DateTime.UtcNow)
            return false;

        _otpStore.Remove(key);
        return true;
    }

    private static string GenerateOtp(int length = 6)
    {
        var max = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(0, max).ToString().PadLeft(length, '0');
    }

    private static string GenerateTemporaryPassword() =>
        $"Wx!{Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))}aA1";

    private static string Base64UrlEncode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Base64UrlDecode(string value)
    {
        var incoming = value.Replace('-', '+').Replace('_', '/');
        switch (incoming.Length % 4)
        {
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
        }
        var bytes = Convert.FromBase64String(incoming);
        return Encoding.UTF8.GetString(bytes);
    }

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
    }
}
