using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Profile;
using WaslX.Application.Features.Profile.Dtos;
using WaslX.Application.Features.Users.UpdateUser;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(ISender sender) : ControllerBase
{
    /// <summary>The signed-in user's own profile, workspace & resolved permissions.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        return (await sender.Send(new GetMeQuery(userId), cancellationToken)).ToActionResult();
    }

    /// <summary>Self-service profile edit (name & phone).</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeInput input, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        // Editing own profile — scope to the caller's own tenant.
        return (await sender.Send(new UpdateUserCommand(userId, input.FullName, input.Phone, User.GetTenantId()), cancellationToken)).ToActionResult();
    }
}
