using Microsoft.AspNetCore.Http;
using WaslX.Domain.Results;

namespace WaslX.Infrastructure.Identity;

public static class UserErrors
{
    public static readonly Error Unauthorized =
        new("User.Unauthorized", "Only Admin can register", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidCredentials =
        new("User.InvalidCredentials", "Invalid email/password", StatusCodes.Status401Unauthorized);

    public static readonly Error DisabledUser =
        new("User.DisabledUser", "Disabled user, please contact your administrator", StatusCodes.Status401Unauthorized);

    public static readonly Error LockedUser =
        new("User.LockedUser", "Locked user, please contact your administrator", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidJwtToken =
        new("User.InvalidJwtToken", "Invalid Jwt token", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidRefreshToken =
        new("User.InvalidRefreshToken", "Invalid refresh token", StatusCodes.Status401Unauthorized);

    public static readonly Error DuplicatedEmail =
        new("User.DuplicatedEmail", "Another user with the same email is already exists", StatusCodes.Status409Conflict);

    public static readonly Error EmailNotConfirmed =
        new("User.EmailNotConfirmed", "Email is not confirmed", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidOtp =
        new("User.InvalidOtp", "Invalid Otp", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidCode =
        new("User.InvalidCode", "Invalid password reset code", StatusCodes.Status400BadRequest);

    public static readonly Error DuplicatedConfirmation =
        new("User.DuplicatedConfirmation", "Email already confirmed", StatusCodes.Status400BadRequest);

    public static readonly Error UserNotFound =
    new("User.UserNotFound", Description: "User is not found", StatusCodes.Status404NotFound);

    public static readonly Error InvalidRoles =
        new("Role.InvalidRoles", "Invalid roles", StatusCodes.Status401Unauthorized);

    public static readonly Error NotCompletedProfile =
        new("User.NotCompletedProfile", "User profile is not completed", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidPassword =
    new Error("InvalidPassword", "The provided old password is incorrect.", StatusCodes.Status400BadRequest);

    public static readonly Error PatientsOnly =
        new("User.PatientsOnly", "Only Patiets can see recordes and some doctors", StatusCodes.Status401Unauthorized);

}
