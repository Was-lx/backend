namespace WaslX.Api.Contracts;

public record ChangePasswordRequest(string OldPassword, string NewPassword);

public record CreateUserRequest(string Email, string FullName, string Role, string? PhoneNumber);

public record AssignRoleRequest(string Role);

public record SetUserStatusRequest(bool IsDisabled);
