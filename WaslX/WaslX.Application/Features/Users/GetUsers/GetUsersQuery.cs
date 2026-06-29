using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Users.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Users.GetUsers;

public record GetUsersQuery(int? TenantId) : IQuery<IReadOnlyList<UserResponse>>;

public class GetUsersQueryHandler(IAuthService authService) : IQueryHandler<GetUsersQuery, IReadOnlyList<UserResponse>>
{
    public Task<Result<IReadOnlyList<UserResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
        authService.GetUsersAsync(request.TenantId, cancellationToken);
}
