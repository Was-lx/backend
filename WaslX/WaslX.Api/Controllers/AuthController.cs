using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Auth.ChangePassword;
using WaslX.Application.Features.Auth.ConfirmEmail;
using WaslX.Application.Features.Auth.ForgetPassword;
using WaslX.Application.Features.Auth.Login;
using WaslX.Application.Features.Auth.Logout;
using WaslX.Application.Features.Auth.RefreshToken;
using WaslX.Application.Features.Auth.Register;
using WaslX.Application.Features.Auth.ResendConfirmationEmail;
using WaslX.Application.Features.Auth.ResetPassword;
using WaslX.Application.Features.Auth.RevokeRefreshToken;
using WaslX.Application.Features.Tenants;
using WaslX.Application.Features.Tenants.Dtos;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>Self-serve sign-up: creates the workspace on a 7-day trial + its Admin, and logs in.</summary>
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SelfServeSignupInput input, CancellationToken cancellationToken) =>
        (await sender.Send(new SignUpCommand(input), cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("revoke-refresh-token")]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] RevokeRefreshTokenCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("resend-confirm-email")]
    public async Task<IActionResult> ResendConfirmEmail([FromBody] ResendConfirmationEmailCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Invalidates the session by revoking the caller's active refresh tokens.
        // The current access token stays valid until it expires (standard JWT trade-off).
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        return (await sender.Send(new LogoutCommand(userId), cancellationToken)).ToActionResult();
    }
}
