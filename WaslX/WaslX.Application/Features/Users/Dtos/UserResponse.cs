namespace WaslX.Application.Features.Users.Dtos;

public record UserResponse(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    int? TenantId,
    IReadOnlyList<string> Roles,
    bool IsDisabled,
    bool IsEmailConfirmed,
    bool IsOwner);
