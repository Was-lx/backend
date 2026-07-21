using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── US-6.5 · AI cost (read-only, cross-tenant) ──
public record GetAiCostQuery(DateTime? From, DateTime? To) : IQuery<AiCostResponse>;
public class GetAiCostQueryHandler(IAiCostService svc) : IQueryHandler<GetAiCostQuery, AiCostResponse>
{
    public Task<Result<AiCostResponse>> Handle(GetAiCostQuery request, CancellationToken cancellationToken) =>
        svc.GetCostAsync(new AiCostQuery(request.From, request.To), cancellationToken);
}

// ── US-6.5 · Budget-alert CRUD ──
public record GetBudgetAlertsQuery() : IQuery<IReadOnlyList<BudgetAlertResponse>>;
public class GetBudgetAlertsQueryHandler(IAiCostService svc) : IQueryHandler<GetBudgetAlertsQuery, IReadOnlyList<BudgetAlertResponse>>
{
    public Task<Result<IReadOnlyList<BudgetAlertResponse>>> Handle(GetBudgetAlertsQuery request, CancellationToken cancellationToken) =>
        svc.GetAlertsAsync(cancellationToken);
}

public record CreateBudgetAlertCommand(CreateBudgetAlertInput Input, PlatformActor Actor) : ICommand<BudgetAlertResponse>;
public class CreateBudgetAlertCommandHandler(IAiCostService svc) : ICommandHandler<CreateBudgetAlertCommand, BudgetAlertResponse>
{
    public Task<Result<BudgetAlertResponse>> Handle(CreateBudgetAlertCommand request, CancellationToken cancellationToken) =>
        svc.CreateAlertAsync(request.Input, request.Actor, cancellationToken);
}

public record UpdateBudgetAlertCommand(int Id, UpdateBudgetAlertInput Input, PlatformActor Actor) : ICommand<BudgetAlertResponse>;
public class UpdateBudgetAlertCommandHandler(IAiCostService svc) : ICommandHandler<UpdateBudgetAlertCommand, BudgetAlertResponse>
{
    public Task<Result<BudgetAlertResponse>> Handle(UpdateBudgetAlertCommand request, CancellationToken cancellationToken) =>
        svc.UpdateAlertAsync(request.Id, request.Input, request.Actor, cancellationToken);
}

public record DeleteBudgetAlertCommand(int Id, PlatformActor Actor) : ICommand;
public class DeleteBudgetAlertCommandHandler(IAiCostService svc) : ICommandHandler<DeleteBudgetAlertCommand>
{
    public Task<Result> Handle(DeleteBudgetAlertCommand request, CancellationToken cancellationToken) =>
        svc.DeleteAlertAsync(request.Id, request.Actor, cancellationToken);
}
