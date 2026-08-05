using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Users.AssignRole;
using WaslX.Application.Features.Users.CreateUser;
using WaslX.Application.Features.Users.GetUsers;
using WaslX.Application.Features.Users.SetUserStatus;
using WaslX.Application.Features.Users.UpdateUser;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        // SuperAdmin (no tenant) sees all users; tenant admins see only their tenant.
        var query = new GetUsersQuery(User.GetTenantId());
        return (await sender.Send(query, cancellationToken)).ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.FullName, request.Role, User.GetTenantId(), request.PhoneNumber);
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.FullName, request.PhoneNumber, User.GetTenantId());
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new AssignRoleCommand(id, request.Role, User.GetTenantId());
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetUserStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new SetUserStatusCommand(id, request.IsDisabled, User.GetTenantId());
        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }
}
