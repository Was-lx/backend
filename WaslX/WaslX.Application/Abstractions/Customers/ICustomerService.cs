using WaslX.Application.Features.Customers.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Customers;

/// <summary>
/// The Contacts directory (tenant-scoped): a paginated, filterable view of every customer that has
/// interacted with the workspace, plus CSV export. Read-only over the operational tables.
/// </summary>
public interface ICustomerService
{
    Task<Result<CustomerListResponse>> GetCustomersAsync(int? tenantId, CustomerListFilter filter, CancellationToken cancellationToken = default);
}
