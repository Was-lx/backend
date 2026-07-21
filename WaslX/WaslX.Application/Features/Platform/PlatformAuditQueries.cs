using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.9 · Global cross-tenant audit read (read-only over the platform audit trail) ──
public record GetPlatformAuditLogsQuery(PlatformAuditQuery Query) : IQuery<PagedResult<PlatformAuditLogResponse>>;
public class GetPlatformAuditLogsQueryHandler(IPlatformAuditService svc) : IQueryHandler<GetPlatformAuditLogsQuery, PagedResult<PlatformAuditLogResponse>>
{
    public Task<Result<PagedResult<PlatformAuditLogResponse>>> Handle(GetPlatformAuditLogsQuery request, CancellationToken cancellationToken) =>
        svc.GetAsync(request.Query, cancellationToken);
}
