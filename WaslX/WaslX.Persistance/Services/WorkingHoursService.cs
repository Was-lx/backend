using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.WorkingHours;
using WaslX.Application.Features.WorkingHours.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class WorkingHoursService(ApplicationDbContext db) : IWorkingHoursService
{
    // ── Company hours ──

    public async Task<Result<CompanyWorkingHoursResponse>> GetCompanyAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CompanyWorkingHoursResponse>(AppErrors.NoTenantContext);

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tid, cancellationToken);
        if (tenant is null)
            return Result.Failure<CompanyWorkingHoursResponse>(AppErrors.TenantNotFound);

        var days = await db.CompanyWorkingDays
            .Where(d => d.TenantId == tid)
            .ToListAsync(cancellationToken);

        // Seed defaults on first read: Sun–Thu 09:00–17:00 working, Fri/Sat off.
        if (days.Count == 0)
        {
            days = BuildDefaultDays(tid);
            db.CompanyWorkingDays.AddRange(days);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(ToResponse(tenant.TimeZoneId, days));
    }

    public async Task<Result<CompanyWorkingHoursResponse>> UpsertCompanyAsync(int? tenantId, UpsertCompanyWorkingHoursRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<CompanyWorkingHoursResponse>(AppErrors.NoTenantContext);

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tid, cancellationToken);
        if (tenant is null)
            return Result.Failure<CompanyWorkingHoursResponse>(AppErrors.TenantNotFound);

        var existing = await db.CompanyWorkingDays
            .Where(d => d.TenantId == tid)
            .ToListAsync(cancellationToken);

        foreach (var dto in request.Days ?? new List<CompanyWorkingDayDto>())
        {
            if (!IsValidDayOfWeek(dto.DayOfWeek))
                return Result.Failure<CompanyWorkingHoursResponse>(AppErrors.InvalidWorkingHours);

            var dow = (DayOfWeek)dto.DayOfWeek;
            TimeOnly? start = null;
            TimeOnly? end = null;

            if (dto.IsWorkingDay)
            {
                if (!TryParseTime(dto.StartTime, out var s) || !TryParseTime(dto.EndTime, out var e) || e <= s)
                    return Result.Failure<CompanyWorkingHoursResponse>(AppErrors.InvalidWorkingHours);
                start = s;
                end = e;
            }

            var row = existing.FirstOrDefault(d => d.DayOfWeek == dow);
            if (row is null)
            {
                row = new CompanyWorkingDay { TenantId = tid, DayOfWeek = dow };
                db.CompanyWorkingDays.Add(row);
                existing.Add(row);
            }
            else
            {
                row.UpdatedAt = DateTime.UtcNow;
            }

            row.IsWorkingDay = dto.IsWorkingDay;
            row.StartTime = start;
            row.EndTime = end;
        }

        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
            tenant.TimeZoneId = request.TimeZoneId.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(tenant.TimeZoneId, existing));
    }

    // ── Shifts ──

    public async Task<Result<IReadOnlyList<ShiftResponse>>> GetShiftsAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<IReadOnlyList<ShiftResponse>>(AppErrors.NoTenantContext);

        // Project the raw TimeOnly values (translatable), then format to "HH:mm" in memory —
        // TimeOnly.ToString(format) has no SQL translation and throws inside an EF projection.
        var raw = await db.Shifts.AsNoTracking()
            .Where(s => s.TenantId == tid)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                Days = s.ShiftDays.OrderBy(d => d.DayOfWeek)
                    .Select(d => new { d.DayOfWeek, d.StartTime, d.EndTime }).ToList(),
                AgentCount = s.AgentShifts.Count
            })
            .ToListAsync(cancellationToken);

        var shifts = raw.Select(s => new ShiftResponse(
            s.Id, s.Name,
            s.Days.Select(d => new ShiftDayDto((int)d.DayOfWeek, d.StartTime.ToString("HH:mm"), d.EndTime.ToString("HH:mm"))).ToList(),
            s.AgentCount)).ToList();

        return Result.Success<IReadOnlyList<ShiftResponse>>(shifts);
    }

    public async Task<Result<ShiftResponse>> CreateShiftAsync(int? tenantId, UpsertShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<ShiftResponse>(AppErrors.NoTenantContext);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<ShiftResponse>(AppErrors.ShiftNameRequired);

        var parsed = await ValidateShiftDaysAsync(tid, request.Days, cancellationToken);
        if (parsed.IsFailure)
            return Result.Failure<ShiftResponse>(parsed.Error);

        var shift = new Shift { TenantId = tid, Name = name };
        foreach (var d in parsed.Value)
            shift.ShiftDays.Add(new ShiftDay { DayOfWeek = d.DayOfWeek, StartTime = d.StartTime, EndTime = d.EndTime });

        db.Shifts.Add(shift);
        await db.SaveChangesAsync(cancellationToken);

        return await GetShiftByIdAsync(tid, shift.Id, cancellationToken);
    }

    public async Task<Result<ShiftResponse>> UpdateShiftAsync(int? tenantId, int id, UpsertShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<ShiftResponse>(AppErrors.NoTenantContext);

        var shift = await db.Shifts
            .Include(s => s.ShiftDays)
            .FirstOrDefaultAsync(s => s.TenantId == tid && s.Id == id, cancellationToken);
        if (shift is null)
            return Result.Failure<ShiftResponse>(AppErrors.ShiftNotFound);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return Result.Failure<ShiftResponse>(AppErrors.ShiftNameRequired);

        var parsed = await ValidateShiftDaysAsync(tid, request.Days, cancellationToken);
        if (parsed.IsFailure)
            return Result.Failure<ShiftResponse>(parsed.Error);

        shift.Name = name;
        shift.UpdatedAt = DateTime.UtcNow;

        // Replace the day windows. Delete old rows in their own SaveChanges first, so re-adding the
        // same (ShiftId, DayOfWeek) key can't collide with the unique index within one SaveChanges
        // (EF doesn't guarantee delete-before-insert ordering for same-table rows).
        db.ShiftDays.RemoveRange(shift.ShiftDays);
        shift.ShiftDays.Clear();
        await db.SaveChangesAsync(cancellationToken);

        foreach (var d in parsed.Value)
            shift.ShiftDays.Add(new ShiftDay { ShiftId = shift.Id, DayOfWeek = d.DayOfWeek, StartTime = d.StartTime, EndTime = d.EndTime });
        await db.SaveChangesAsync(cancellationToken);

        return await GetShiftByIdAsync(tid, shift.Id, cancellationToken);
    }

    public async Task<Result> DeleteShiftAsync(int? tenantId, int id, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure(AppErrors.NoTenantContext);

        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.TenantId == tid && s.Id == id, cancellationToken);
        if (shift is null)
            return Result.Failure(AppErrors.ShiftNotFound);

        // Restrict FKs: clear child rows before removing the shift.
        var shiftDays = await db.ShiftDays.Where(d => d.ShiftId == id).ToListAsync(cancellationToken);
        var agentShifts = await db.AgentShifts.Where(a => a.ShiftId == id).ToListAsync(cancellationToken);
        db.ShiftDays.RemoveRange(shiftDays);
        db.AgentShifts.RemoveRange(agentShifts);
        db.Shifts.Remove(shift);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // ── Helpers ──

    private async Task<Result<ShiftResponse>> GetShiftByIdAsync(int tenantId, int id, CancellationToken cancellationToken)
    {
        var raw = await db.Shifts.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.Name,
                Days = s.ShiftDays.OrderBy(d => d.DayOfWeek)
                    .Select(d => new { d.DayOfWeek, d.StartTime, d.EndTime }).ToList(),
                AgentCount = s.AgentShifts.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw is null)
            return Result.Failure<ShiftResponse>(AppErrors.ShiftNotFound);

        // Format TimeOnly -> "HH:mm" in memory (untranslatable in SQL).
        var shift = new ShiftResponse(
            raw.Id, raw.Name,
            raw.Days.Select(d => new ShiftDayDto((int)d.DayOfWeek, d.StartTime.ToString("HH:mm"), d.EndTime.ToString("HH:mm"))).ToList(),
            raw.AgentCount);
        return Result.Success(shift);
    }

    /// <summary>
    /// Parses + validates the requested shift days: reject End&lt;=Start, and require each day to fall on a
    /// company working day with the window sitting inside that day's company hours.
    /// </summary>
    private async Task<Result<List<ShiftDay>>> ValidateShiftDaysAsync(int tenantId, IReadOnlyList<ShiftDayDto>? days, CancellationToken cancellationToken)
    {
        var company = await db.CompanyWorkingDays.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var result = new List<ShiftDay>();
        foreach (var dto in days ?? new List<ShiftDayDto>())
        {
            if (!IsValidDayOfWeek(dto.DayOfWeek))
                return Result.Failure<List<ShiftDay>>(AppErrors.InvalidWorkingHours);

            if (!TryParseTime(dto.StartTime, out var start) || !TryParseTime(dto.EndTime, out var end) || end <= start)
                return Result.Failure<List<ShiftDay>>(AppErrors.InvalidWorkingHours);

            var dow = (DayOfWeek)dto.DayOfWeek;
            var companyDay = company.FirstOrDefault(c => c.DayOfWeek == dow);
            if (companyDay is null || !companyDay.IsWorkingDay || companyDay.StartTime is not { } cStart || companyDay.EndTime is not { } cEnd)
                return Result.Failure<List<ShiftDay>>(AppErrors.ShiftOutsideCompanyHours);

            if (start < cStart || end > cEnd)
                return Result.Failure<List<ShiftDay>>(AppErrors.ShiftOutsideCompanyHours);

            result.Add(new ShiftDay { DayOfWeek = dow, StartTime = start, EndTime = end });
        }

        return Result.Success(result);
    }

    private static List<CompanyWorkingDay> BuildDefaultDays(int tenantId)
    {
        var work = new TimeOnly(9, 0);
        var off = new TimeOnly(17, 0);
        var days = new List<CompanyWorkingDay>();
        foreach (DayOfWeek dow in Enum.GetValues<DayOfWeek>())
        {
            var isWorking = dow is not (DayOfWeek.Friday or DayOfWeek.Saturday);
            days.Add(new CompanyWorkingDay
            {
                TenantId = tenantId,
                DayOfWeek = dow,
                IsWorkingDay = isWorking,
                StartTime = isWorking ? work : null,
                EndTime = isWorking ? off : null,
            });
        }
        return days;
    }

    private static CompanyWorkingHoursResponse ToResponse(string timeZoneId, IReadOnlyList<CompanyWorkingDay> days)
    {
        var ordered = days
            .OrderBy(d => (int)d.DayOfWeek)
            .Select(d => new CompanyWorkingDayDto(
                (int)d.DayOfWeek,
                d.IsWorkingDay,
                d.StartTime?.ToString("HH:mm"),
                d.EndTime?.ToString("HH:mm")))
            .ToList();
        return new CompanyWorkingHoursResponse(timeZoneId, ordered);
    }

    private static bool IsValidDayOfWeek(int value) => value is >= 0 and <= 6;

    private static bool TryParseTime(string? value, out TimeOnly time)
    {
        time = default;
        return !string.IsNullOrWhiteSpace(value)
            && TimeOnly.TryParseExact(value.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}
