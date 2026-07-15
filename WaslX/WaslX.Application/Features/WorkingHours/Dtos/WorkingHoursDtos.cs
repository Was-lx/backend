using System.Collections.Generic;

namespace WaslX.Application.Features.WorkingHours.Dtos;

/// <summary>One day of the company's operating hours. Times are "HH:mm" strings (null when the day is off).</summary>
public record CompanyWorkingDayDto(int DayOfWeek, bool IsWorkingDay, string? StartTime, string? EndTime);

/// <summary>The full company working-hours screen: the tenant time zone + the 7 days.</summary>
public record CompanyWorkingHoursResponse(string TimeZoneId, IReadOnlyList<CompanyWorkingDayDto> Days);

/// <summary>Upsert payload for company hours. TimeZoneId is optional (unchanged when null).</summary>
public record UpsertCompanyWorkingHoursRequest(string? TimeZoneId, IReadOnlyList<CompanyWorkingDayDto> Days);

/// <summary>One day's window inside a shift. Times are "HH:mm" strings.</summary>
public record ShiftDayDto(int DayOfWeek, string StartTime, string EndTime);

/// <summary>A named shift with its per-day windows and how many agents are attached.</summary>
public record ShiftResponse(int Id, string Name, IReadOnlyList<ShiftDayDto> Days, int AgentCount);

/// <summary>Create/update payload for a shift.</summary>
public record UpsertShiftRequest(string Name, IReadOnlyList<ShiftDayDto> Days);
