using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Billing.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Billing;

public record GetMySubscriptionQuery(int TenantId) : IQuery<SubscriptionResponse>;
public class GetMySubscriptionQueryHandler(ISubscriptionService svc) : IQueryHandler<GetMySubscriptionQuery, SubscriptionResponse>
{
    public Task<Result<SubscriptionResponse>> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken) =>
        svc.GetMineAsync(request.TenantId, cancellationToken);
}

public record UpgradeSubscriptionCommand(int TenantId, UpgradeInput Input) : ICommand<SubscriptionResponse>;
public class UpgradeSubscriptionCommandHandler(ISubscriptionService svc) : ICommandHandler<UpgradeSubscriptionCommand, SubscriptionResponse>
{
    public Task<Result<SubscriptionResponse>> Handle(UpgradeSubscriptionCommand request, CancellationToken cancellationToken) =>
        svc.UpgradeAsync(request.TenantId, request.Input, cancellationToken);
}

public record CancelSubscriptionCommand(int TenantId) : ICommand;
public class CancelSubscriptionCommandHandler(ISubscriptionService svc) : ICommandHandler<CancelSubscriptionCommand>
{
    public Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken) =>
        svc.CancelAsync(request.TenantId, cancellationToken);
}

public record SetPaymentMethodCommand(int TenantId, AddCardInput Input) : ICommand<PaymentMethodDto>;
public class SetPaymentMethodCommandHandler(ISubscriptionService svc) : ICommandHandler<SetPaymentMethodCommand, PaymentMethodDto>
{
    public Task<Result<PaymentMethodDto>> Handle(SetPaymentMethodCommand request, CancellationToken cancellationToken) =>
        svc.SetPaymentMethodAsync(request.TenantId, request.Input, cancellationToken);
}
