using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Infrastructure.Identity
{
    internal class AuthSerive : IAuthService
    {
        public Task<Result> AddRefreshTokenAsync(string userId, string token, DateTime expiresOn)
        {
            throw new NotImplementedException();
        }

        public Task<Result> CheckPasswordAsync(string userId, string password)
        {
            throw new NotImplementedException();
        }

        public Task<Result> ConfirmEmailAsync(string email, string otp)
        {
            throw new NotImplementedException();
        }

        public Task<Result<string>> CreateConfirmedUserAsync(string email, string fullName, string role, string? phone = null)
        {
            throw new NotImplementedException();
        }

        public Task<Result<string>> CreateUserAsync(string email, string password, string fullName, string role, string? phoneNumber = null)
        {
            throw new NotImplementedException();
        }

        public Task<Result<AuthResponse>> FindByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Result<AuthResponse>> FindByIdAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<Result<AuthResponse>> FindByRefreshTokenAsync(string token)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IReadOnlyList<string>>> GetRolesAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<Result> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task<Result> RevokeRefreshTokenAsync(string userId, string token)
        {
            throw new NotImplementedException();
        }

        public Task<Result> SendAccountActivationOtpAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Result> SendEmailConfirmationOtpAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Result> SendPasswordResetOtpAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task SendRejectionEmailAsync(string email, string name, string reason)
        {
            throw new NotImplementedException();
        }

        public Task<Result> ValidateRefreshTokenAsync(string userId, string token)
        {
            throw new NotImplementedException();
        }
    }
}
