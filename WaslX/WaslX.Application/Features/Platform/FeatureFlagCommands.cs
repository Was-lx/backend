using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.7 · Feature flags (global + per-tenant, per-tenant overrides global) ──
public record GetFeatureFlagsQuery() : IQuery<IReadOnlyList<FeatureFlagResponse>>;
public class GetFeatureFlagsQueryHandler(IFeatureFlagService svc) : IQueryHandler<GetFeatureFlagsQuery, IReadOnlyList<FeatureFlagResponse>>
{
    public Task<Result<IReadOnlyList<FeatureFlagResponse>>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(cancellationToken);
}

public record ResolveFeatureFlagQuery(string Key, int? TenantId) : IQuery<ResolvedFeatureFlagResponse>;
public class ResolveFeatureFlagQueryHandler(IFeatureFlagService svc) : IQueryHandler<ResolveFeatureFlagQuery, ResolvedFeatureFlagResponse>
{
    public Task<Result<ResolvedFeatureFlagResponse>> Handle(ResolveFeatureFlagQuery request, CancellationToken cancellationToken) =>
        svc.ResolveAsync(request.Key, request.TenantId, cancellationToken);
}

public record UpsertFeatureFlagCommand(UpsertFeatureFlagInput Input, PlatformActor Actor) : ICommand<FeatureFlagResponse>;
public class UpsertFeatureFlagCommandHandler(IFeatureFlagService svc) : ICommandHandler<UpsertFeatureFlagCommand, FeatureFlagResponse>
{
    public Task<Result<FeatureFlagResponse>> Handle(UpsertFeatureFlagCommand request, CancellationToken cancellationToken) =>
        svc.UpsertAsync(request.Input, request.Actor, cancellationToken);
}

public record ToggleFeatureFlagCommand(int Id, ToggleFeatureFlagInput Input, PlatformActor Actor) : ICommand<FeatureFlagResponse>;
public class ToggleFeatureFlagCommandHandler(IFeatureFlagService svc) : ICommandHandler<ToggleFeatureFlagCommand, FeatureFlagResponse>
{
    public Task<Result<FeatureFlagResponse>> Handle(ToggleFeatureFlagCommand request, CancellationToken cancellationToken) =>
        svc.ToggleAsync(request.Id, request.Input, request.Actor, cancellationToken);
}

public record DeleteFeatureFlagCommand(int Id, PlatformActor Actor) : ICommand;
public class DeleteFeatureFlagCommandHandler(IFeatureFlagService svc) : ICommandHandler<DeleteFeatureFlagCommand>
{
    public Task<Result> Handle(DeleteFeatureFlagCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAsync(request.Id, request.Actor, cancellationToken);
}
