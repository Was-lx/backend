using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.WorkingHours;
using WaslX.Application.Features.WorkingHours.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>Working hours — company operating hours + named shifts. Admin/Manager only.</summary>
[ApiController]
[Route("api/working-hours")]
[Authorize(Roles = "Admin,Manager")]
public class WorkingHoursController(ISender sender) : ControllerBase
{
    [HttpGet("company")]
    public async Task<IActionResult> GetCompany(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyWorkingHoursQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpPut("company")]
    public async Task<IActionResult> UpsertCompany([FromBody] UpsertCompanyWorkingHoursRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpsertCompanyWorkingHoursCommand(User.GetTenantId(), request), cancellationToken)).ToActionResult();

    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(CancellationToken cancellationToken) =>
        (await sender.Send(new GetShiftsQuery(User.GetTenantId()), cancellationToken)).ToActionResult();

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] UpsertShiftRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreateShiftCommand(User.GetTenantId(), request), cancellationToken)).ToActionResult();

    [HttpPut("shifts/{id:int}")]
    public async Task<IActionResult> UpdateShift(int id, [FromBody] UpsertShiftRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateShiftCommand(User.GetTenantId(), id, request), cancellationToken)).ToActionResult();

    [HttpDelete("shifts/{id:int}")]
    public async Task<IActionResult> DeleteShift(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteShiftCommand(User.GetTenantId(), id), cancellationToken)).ToActionResult();
}
