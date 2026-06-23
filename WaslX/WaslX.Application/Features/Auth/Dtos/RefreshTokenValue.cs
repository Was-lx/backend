namespace WaslX.Application.Features.Auth.Dtos;

public record RefreshTokenValue(string Token, DateTime ExpiresOn);
