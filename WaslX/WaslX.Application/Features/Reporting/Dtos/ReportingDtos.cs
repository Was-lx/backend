namespace WaslX.Application.Features.Reporting.Dtos;

// The DTOs below match the reporting dashboard's contract exactly (camelCased by System.Text.Json).
// Every figure is computed LIVE from the operational tables (conversations / messages / assignments);
// none of it reads the never-populated AgentPerformance snapshot table.

// ── Overview / KPI band ──────────────────────────────────────────────────────
/// <summary>One KPI tile: a value, the previous-period value + delta%, and a sparkline series.
/// <c>Format</c> ∈ number | duration (seconds) | percent. Nulls render as "—".</summary>
public record KpiDto(
    string Key,
    double? Value,
    double? PreviousValue,
    double? DeltaPct,
    string Format,
    IReadOnlyList<double> Spark);

public record OverviewResponse(IReadOnlyList<KpiDto> Kpis);

// ── Conversation volume (per-day inbound vs outbound messages) ────────────────
public record VolumePointDto(string Date, double Inbound, double Outbound);
public record ConversationVolumeResponse(IReadOnlyList<VolumePointDto> Points);

// ── Agent leaderboard ────────────────────────────────────────────────────────
public record AgentRowDto(
    int UserId,
    string Name,
    int Handled,
    double AvgResponseSec,
    double ResolutionRate,
    int ActiveChats,
    IReadOnlyList<double> Spark);

public record AgentReportResponse(IReadOnlyList<AgentRowDto> Agents);

// ── Response & resolution ────────────────────────────────────────────────────
public record ResponsePointDto(string Date, double FirstResponseSec, double ResolutionSec);
public record ResponseBucketDto(string Label, int Count);
public record ResponseTimesResponse(
    IReadOnlyList<ResponsePointDto> Series,
    IReadOnlyList<ResponseBucketDto> Distribution,
    double SlaPct);

// ── Assignment split (routing) ───────────────────────────────────────────────
public record RoutingSliceDto(string Method, int Count);
public record RoutingStatsResponse(IReadOnlyList<RoutingSliceDto> Slices);

// ── Export ───────────────────────────────────────────────────────────────────
public record ReportFile(byte[] Bytes, string ContentType, string FileName);
