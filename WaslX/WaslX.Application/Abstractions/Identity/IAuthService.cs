using System;
using System.Collections.Generic;
using System.Text;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Identity;

public interface IAuthService
{
    Task<Result<AuthResponse>> FindByEmailAsync(string email);
    Task<Result<AuthResponse>> FindByIdAsync(string userId);
    Task<Result<AuthResponse>> FindByRefreshTokenAsync(string token);

    Task<Result<string>> CreateUserAsync(string email, string password, string fullName, string role, string? phoneNumber = null);
    Task<Result> CheckPasswordAsync(string userId, string password);
    Task<Result<IReadOnlyList<string>>> GetRolesAsync(string userId);

    // OTP-based email confirmation
    Task<Result> SendEmailConfirmationOtpAsync(string email);
    Task<Result> ConfirmEmailAsync(string email, string otp);

    // OTP-based password reset
    Task<Result> SendPasswordResetOtpAsync(string email);
    Task<Result> ResetPasswordAsync(string email, string otp, string newPassword);

    // Donor account activation (creates confirmed user + sends set-password OTP)
    Task<Result<string>> CreateConfirmedUserAsync(string email, string fullName, string role, string? phone = null);
    Task<Result> SendAccountActivationOtpAsync(string email);
    Task SendRejectionEmailAsync(string email, string name, string reason);

    // Refresh tokens
    Task<Result> AddRefreshTokenAsync(string userId, string token, DateTime expiresOn);
    Task<Result> ValidateRefreshTokenAsync(string userId, string token);
    Task<Result> RevokeRefreshTokenAsync(string userId, string token);
}
