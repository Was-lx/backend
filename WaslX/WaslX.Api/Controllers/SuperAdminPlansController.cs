using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Contracts;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Plans;
using WaslX.Application.Features.Plans.Dtos;

namespace WaslX.Api.Controllers;

[ApiController]
[Route("api/superadmin/plans")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminPlansController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        (await sender.Send(new GetPlansQuery(), cancellationToken)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetPlanByIdQuery(id), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPlanRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CreatePlanCommand(request), cancellationToken)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertPlanRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdatePlanCommand(id, request), cancellationToken)).ToActionResult();

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetPlanActiveRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetPlanActiveCommand(id, request.IsActive), cancellationToken)).ToActionResult();
}
