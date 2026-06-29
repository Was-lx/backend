using WaslX.Application.Features.Auth.Dtos;

namespace WaslX.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    AccessToken GenerateAccessToken(string userId, string email, string fullName, IEnumerable<string> roles, int? tenantId = null);
    RefreshTokenValue GenerateRefreshToken();

    // Returns the user id (sub) from a token whose signature is valid, ignoring expiry.
    // Used by the refresh-token flow. Returns null when the token is invalid.
    string? ValidateTokenAndGetUserId(string token);
}