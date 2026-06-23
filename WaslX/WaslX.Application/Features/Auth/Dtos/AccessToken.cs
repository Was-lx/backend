namespace WaslX.Application.Features.Auth.Dtos;

public record AccessToken(string Token, DateTime ExpiresOn);
