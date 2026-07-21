using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.4 · Global usage (read-only, cross-tenant) ──
public record GetPlatformUsageQuery(DateTime? From, DateTime? To) : IQuery<PlatformUsageResponse>;

public class GetPlatformUsageQueryHandler(IPlatformMetricsService svc) : IQueryHandler<GetPlatformUsageQuery, PlatformUsageResponse>
{
    public Task<Result<PlatformUsageResponse>> Handle(GetPlatformUsageQuery request, CancellationToken cancellationToken) =>
        svc.GetUsageAsync(new PlatformUsageQuery(request.From, request.To), cancellationToken);
}
