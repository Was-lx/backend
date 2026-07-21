using WaslX.Application.Features.Auth.Dtos;

namespace WaslX.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    AccessToken GenerateAccessToken(string userId, string email, string fullName, IEnumerable<string> roles, int? tenantId = null, int? domainUserId = null);

    // Mints a SHORT-LIVED tenant access token for an audited SuperAdmin impersonation session (US-6.8).
    // Identical to a normal tenant token (tenantId + duid + role claims) but its expiry is bounded by the
    // session window and it carries an "imp" claim = the ImpersonationSession id so the session is traceable.
    AccessToken GenerateImpersonationToken(string userId, string email, string fullName, IEnumerable<string> roles, int tenantId, int? domainUserId, DateTime expiresAt, string impersonationSessionId);

    RefreshTokenValue GenerateRefreshToken();

    // Returns the user id (sub) from a token whose signature is valid, ignoring expiry.
    // Used by the refresh-token flow. Returns null when the token is invalid.
    string? ValidateTokenAndGetUserId(string token);
}