using WaslX.Application.Abstractions.Customers;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Customers.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Customers;

public record GetCustomersQuery(int? TenantId, CustomerListFilter Filter) : IQuery<CustomerListResponse>;
public class GetCustomersQueryHandler(ICustomerService svc) : IQueryHandler<GetCustomersQuery, CustomerListResponse>
{
    public Task<Result<CustomerListResponse>> Handle(GetCustomersQuery request, CancellationToken cancellationToken) =>
        svc.GetCustomersAsync(request.TenantId, request.Filter, cancellationToken);
}
