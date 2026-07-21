using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaslX.Api.Extensions;
using WaslX.Application.Features.Customers;
using WaslX.Application.Features.Customers.Dtos;

namespace WaslX.Api.Controllers;

/// <summary>
/// The Contacts directory (FR-CONV / customer data): a paginated, filterable listing of every
/// customer in the tenant. Tenant-scoped; read-only. CSV/Excel export is built on the client from
/// the returned rows (request a large pageSize to pull the full filtered set).
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Manager,Agent")]
[Route("api/customers")]
public class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int? tagId,
        [FromQuery] string? assignedUserId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var filter = new CustomerListFilter(search, tagId, assignedUserId, dateFrom, dateTo, page, pageSize);
        return (await sender.Send(new GetCustomersQuery(User.GetTenantId(), filter), cancellationToken)).ToActionResult();
    }
}
