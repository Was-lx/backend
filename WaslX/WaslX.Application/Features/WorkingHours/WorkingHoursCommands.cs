using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.WorkingHours;
using WaslX.Application.Features.WorkingHours.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.WorkingHours;

// ── Company hours ──
public record GetCompanyWorkingHoursQuery(int? TenantId) : IQuery<CompanyWorkingHoursResponse>;
public class GetCompanyWorkingHoursQueryHandler(IWorkingHoursService svc) : IQueryHandler<GetCompanyWorkingHoursQuery, CompanyWorkingHoursResponse>
{
    public Task<Result<CompanyWorkingHoursResponse>> Handle(GetCompanyWorkingHoursQuery request, CancellationToken cancellationToken) =>
        svc.GetCompanyAsync(request.TenantId, cancellationToken);
}

public record UpsertCompanyWorkingHoursCommand(int? TenantId, UpsertCompanyWorkingHoursRequest Request) : ICommand<CompanyWorkingHoursResponse>;
public class UpsertCompanyWorkingHoursCommandHandler(IWorkingHoursService svc) : ICommandHandler<UpsertCompanyWorkingHoursCommand, CompanyWorkingHoursResponse>
{
    public Task<Result<CompanyWorkingHoursResponse>> Handle(UpsertCompanyWorkingHoursCommand request, CancellationToken cancellationToken) =>
        svc.UpsertCompanyAsync(request.TenantId, request.Request, cancellationToken);
}

// ── Shifts ──
public record GetShiftsQuery(int? TenantId) : IQuery<IReadOnlyList<ShiftResponse>>;
public class GetShiftsQueryHandler(IWorkingHoursService svc) : IQueryHandler<GetShiftsQuery, IReadOnlyList<ShiftResponse>>
{
    public Task<Result<IReadOnlyList<ShiftResponse>>> Handle(GetShiftsQuery request, CancellationToken cancellationToken) =>
        svc.GetShiftsAsync(request.TenantId, cancellationToken);
}

public record CreateShiftCommand(int? TenantId, UpsertShiftRequest Request) : ICommand<ShiftResponse>;
public class CreateShiftCommandHandler(IWorkingHoursService svc) : ICommandHandler<CreateShiftCommand, ShiftResponse>
{
    public Task<Result<ShiftResponse>> Handle(CreateShiftCommand request, CancellationToken cancellationToken) =>
        svc.CreateShiftAsync(request.TenantId, request.Request, cancellationToken);
}

public record UpdateShiftCommand(int? TenantId, int Id, UpsertShiftRequest Request) : ICommand<ShiftResponse>;
public class UpdateShiftCommandHandler(IWorkingHoursService svc) : ICommandHandler<UpdateShiftCommand, ShiftResponse>
{
    public Task<Result<ShiftResponse>> Handle(UpdateShiftCommand request, CancellationToken cancellationToken) =>
        svc.UpdateShiftAsync(request.TenantId, request.Id, request.Request, cancellationToken);
}

public record DeleteShiftCommand(int? TenantId, int Id) : ICommand;
public class DeleteShiftCommandHandler(IWorkingHoursService svc) : ICommandHandler<DeleteShiftCommand>
{
    public Task<Result> Handle(DeleteShiftCommand request, CancellationToken cancellationToken) =>
        svc.DeleteShiftAsync(request.TenantId, request.Id, cancellationToken);
}
