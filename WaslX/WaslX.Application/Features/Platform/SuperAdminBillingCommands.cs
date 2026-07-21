using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Billing.Dtos;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Platform;

// ── Change plan (US-6.3) ──
public record ChangeTenantPlanCommand(int TenantId, ChangeTenantPlanInput Input, PlatformActor Actor) : ICommand;
public class ChangeTenantPlanCommandHandler(ISuperAdminBillingService svc) : ICommandHandler<ChangeTenantPlanCommand>
{
    public Task<Result> Handle(ChangeTenantPlanCommand request, CancellationToken cancellationToken) =>
        svc.ChangePlanAsync(request.TenantId, request.Input, request.Actor, cancellationToken);
}

// ── Invoices (US-6.3) ──
public record GetTenantInvoicesQuery(int TenantId) : IQuery<IReadOnlyList<InvoiceDto>>;
public class GetTenantInvoicesQueryHandler(ISuperAdminBillingService svc) : IQueryHandler<GetTenantInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public Task<Result<IReadOnlyList<InvoiceDto>>> Handle(GetTenantInvoicesQuery request, CancellationToken cancellationToken) =>
        svc.GetInvoicesAsync(request.TenantId, cancellationToken);
}

public record GenerateInvoiceCommand(int TenantId, GenerateInvoiceInput Input, PlatformActor Actor) : ICommand<InvoiceDto>;
public class GenerateInvoiceCommandHandler(ISuperAdminBillingService svc) : ICommandHandler<GenerateInvoiceCommand, InvoiceDto>
{
    public Task<Result<InvoiceDto>> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken) =>
        svc.GenerateInvoiceAsync(request.TenantId, request.Input, request.Actor, cancellationToken);
}

public record MarkInvoicePaidCommand(int InvoiceId, PlatformActor Actor) : ICommand<InvoiceDto>;
public class MarkInvoicePaidCommandHandler(ISuperAdminBillingService svc) : ICommandHandler<MarkInvoicePaidCommand, InvoiceDto>
{
    public Task<Result<InvoiceDto>> Handle(MarkInvoicePaidCommand request, CancellationToken cancellationToken) =>
        svc.MarkPaidAsync(request.InvoiceId, request.Actor, cancellationToken);
}
