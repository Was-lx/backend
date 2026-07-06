using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Plans.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Plans;

// ── Queries ──
public record GetPlansQuery() : IQuery<IReadOnlyList<PlanResponse>>;
public class GetPlansQueryHandler(ISubscriptionPlanService svc) : IQueryHandler<GetPlansQuery, IReadOnlyList<PlanResponse>>
{
    public Task<Result<IReadOnlyList<PlanResponse>>> Handle(GetPlansQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(cancellationToken);
}

public record GetPublicPlansQuery() : IQuery<IReadOnlyList<PlanResponse>>;
public class GetPublicPlansQueryHandler(ISubscriptionPlanService svc) : IQueryHandler<GetPublicPlansQuery, IReadOnlyList<PlanResponse>>
{
    public Task<Result<IReadOnlyList<PlanResponse>>> Handle(GetPublicPlansQuery request, CancellationToken cancellationToken) =>
        svc.GetPublicAsync(cancellationToken);
}

public record GetPlanByIdQuery(int Id) : IQuery<PlanResponse>;
public class GetPlanByIdQueryHandler(ISubscriptionPlanService svc) : IQueryHandler<GetPlanByIdQuery, PlanResponse>
{
    public Task<Result<PlanResponse>> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken) =>
        svc.GetByIdAsync(request.Id, cancellationToken);
}

// ── Commands ──
public record CreatePlanCommand(UpsertPlanRequest Request) : ICommand<PlanResponse>;
public class CreatePlanCommandHandler(ISubscriptionPlanService svc) : ICommandHandler<CreatePlanCommand, PlanResponse>
{
    public Task<Result<PlanResponse>> Handle(CreatePlanCommand request, CancellationToken cancellationToken) =>
        svc.CreateAsync(request.Request, cancellationToken);
}

public record UpdatePlanCommand(int Id, UpsertPlanRequest Request) : ICommand<PlanResponse>;
public class UpdatePlanCommandHandler(ISubscriptionPlanService svc) : ICommandHandler<UpdatePlanCommand, PlanResponse>
{
    public Task<Result<PlanResponse>> Handle(UpdatePlanCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAsync(request.Id, request.Request, cancellationToken);
}

public record SetPlanActiveCommand(int Id, bool IsActive) : ICommand;
public class SetPlanActiveCommandHandler(ISubscriptionPlanService svc) : ICommandHandler<SetPlanActiveCommand>
{
    public Task<Result> Handle(SetPlanActiveCommand request, CancellationToken cancellationToken) =>
        svc.SetActiveAsync(request.Id, request.IsActive, cancellationToken);
}
