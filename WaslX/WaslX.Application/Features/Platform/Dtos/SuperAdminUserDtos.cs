namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>One row of the SuperAdmin admins list (a platform-level user in the SuperAdmin role).</summary>
public record SuperAdminUserResponse(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsDisabled,
    bool EmailConfirmed);

/// <summary>Create a new platform SuperAdmin (Identity user, no tenant).</summary>
public record CreateSuperAdminInput(string Email, string FullName, string? Phone);

/// <summary>Enable/disable a platform SuperAdmin.</summary>
public record SetSuperAdminStatusInput(bool IsDisabled);
