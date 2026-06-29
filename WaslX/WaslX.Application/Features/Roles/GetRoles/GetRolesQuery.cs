using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Roles.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Roles.GetRoles;

public record GetRolesQuery() : IQuery<IReadOnlyList<RoleResponse>>;

public class GetRolesQueryHandler(IAuthService authService) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleResponse>>
{
    public Task<Result<IReadOnlyList<RoleResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken) =>
        authService.GetRolesListAsync();
}
