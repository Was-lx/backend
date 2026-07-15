using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.WorkingHours.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.WorkingHours;

/// <summary>Working hours (tenant-scoped): company operating hours + named shifts that agents work.</summary>
public interface IWorkingHoursService
{
    Task<Result<CompanyWorkingHoursResponse>> GetCompanyAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<CompanyWorkingHoursResponse>> UpsertCompanyAsync(int? tenantId, UpsertCompanyWorkingHoursRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ShiftResponse>>> GetShiftsAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<Result<ShiftResponse>> CreateShiftAsync(int? tenantId, UpsertShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShiftResponse>> UpdateShiftAsync(int? tenantId, int id, UpsertShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteShiftAsync(int? tenantId, int id, CancellationToken cancellationToken = default);
}
