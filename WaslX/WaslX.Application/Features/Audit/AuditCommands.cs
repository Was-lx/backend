using WaslX.Application.Abstractions.Audit;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Audit.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Audit;

// ── Queries ──
// The audit log is immutable: read-only query surface, no create/update/delete commands.
public record GetAuditLogsQuery(int? TenantId, AuditQuery Query) : IQuery<PagedResult<AuditLogResponse>>;

public class GetAuditLogsQueryHandler(IAuditService svc) : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogResponse>>
{
    public Task<Result<PagedResult<AuditLogResponse>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken) =>
        svc.GetAsync(request.TenantId, request.Query, cancellationToken);
}
