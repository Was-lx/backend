using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Features.Permissions.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Permissions;

public record GetPermissionMatrixQuery(int TenantId) : IQuery<PermissionMatrixResponse>;
public class GetPermissionMatrixQueryHandler(IPermissionService svc) : IQueryHandler<GetPermissionMatrixQuery, PermissionMatrixResponse>
{
    public Task<Result<PermissionMatrixResponse>> Handle(GetPermissionMatrixQuery request, CancellationToken cancellationToken) =>
        svc.GetTenantMatrixAsync(request.TenantId, cancellationToken);
}

public record UpdatePermissionMatrixCommand(int TenantId, IReadOnlyList<PermissionUpdateItem> Changes) : ICommand;
public class UpdatePermissionMatrixCommandHandler(IPermissionService svc) : ICommandHandler<UpdatePermissionMatrixCommand>
{
    public Task<Result> Handle(UpdatePermissionMatrixCommand request, CancellationToken cancellationToken) =>
        svc.UpdateTenantMatrixAsync(request.TenantId, request.Changes, cancellationToken);
}
