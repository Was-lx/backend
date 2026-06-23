using WaslX.Application.Features.Auth.Dtos;

namespace WaslX.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    AccessToken GenerateAccessToken(string userId, string email, string fullName, IEnumerable<string> roles);
    RefreshTokenValue GenerateRefreshToken();
}