using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WaslX.Application.Abstractions.Reporting;
using WaslX.Application.Features.Reporting.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Read-only reporting aggregation, computed LIVE from the operational tables (conversations,
/// messages, assignments) — nothing here writes to the DB, and it never reads the AgentPerformance
/// snapshot table (which is not populated). Tenant isolation is manual (every query filters
/// TenantId); Agents are scoped to their own conversations; an optional teamId scopes to a Group.
/// Durations are seconds; rates/shares are 0–100 percentages.
/// </summary>
internal sealed class ReportingService(ApplicationDbContext db) : IReportingService
{
    static ReportingService()
    {
        // QuestPDF is free for open-source / small business use under the Community licence.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // SLA target for a first response (seconds). Answered within this counts toward "within SLA".
    private const double SlaSeconds = 300; // 5 minutes

    private static bool IsAgent(string? role) =>
        string.Equals(role, "Agent", StringComparison.OrdinalIgnoreCase);

    /// <summary>Tenant + role (Agent → own) + optional team scoped conversation query, no date filter.</summary>
    private IQueryable<Conversation> BaseConversations(int tid, string? role, int? uid, int? teamId)
    {
        var q = db.Conversations.AsNoTracking().Where(c => c.TenantId == tid && !c.IsDeleted);
        if (IsAgent(role)) q = q.Where(c => c.AssignedUserId == uid);
        if (teamId is { } tm) q = q.Where(c => c.GroupId == tm);
        return q;
    }

    // A day-by-day label list covering [from,to] inclusive, capped so a silly range can't explode.
    private static List<DateTime> DayBuckets(DateTime from, DateTime to)
    {
        var days = new List<DateTime>();
        for (var d = from.Date; d <= to.Date && days.Count < 400; d = d.AddDays(1))
            days.Add(d);
        return days;
    }

    private static double? Delta(double cur, double prev) =>
        prev == 0 ? null : Math.Round((cur - prev) / prev * 100, 1);

    // Per-conversation timing, fetched once per period. Correlated sub-selects (FirstOrDefault/Any)
    // are well-supported by EF; all averaging/bucketing then happens in memory (translation-safe).
    private sealed class ConvTiming
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ConversationStatus Status { get; set; }
        public DateTime? FirstAgentAt { get; set; }
        public bool HasAi { get; set; }
    }

    private Task<List<ConvTiming>> FetchTimingsAsync(IQueryable<Conversation> baseConv, DateTime from, DateTime to, CancellationToken ct) =>
        baseConv.Where(c => c.CreatedAt >= from && c.CreatedAt <= to)
            .Select(c => new ConvTiming
            {
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Status = c.Status,
                FirstAgentAt = db.Messages
                    .Where(m => m.ConversationId == c.Id && m.SenderType == SenderType.Agent)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => (DateTime?)m.Timestamp)
                    .FirstOrDefault(),
                HasAi = db.Messages.Any(m => m.ConversationId == c.Id && m.SenderType == SenderType.AI),
            })
            .ToListAsync(ct);

    private static double AvgFirstResponse(IEnumerable<ConvTiming> t)
    {
        var xs = t.Where(x => x.FirstAgentAt is not null)
            .Select(x => (x.FirstAgentAt!.Value - x.CreatedAt).TotalSeconds)
            .Where(s => s >= 0).ToList();
        return xs.Count == 0 ? 0 : Math.Round(xs.Average(), 0);
    }

    private static double AvgResolution(IEnumerable<ConvTiming> t)
    {
        var xs = t.Where(x => x.Status == ConversationStatus.Resolved && x.UpdatedAt is not null)
            .Select(x => (x.UpdatedAt!.Value - x.CreatedAt).TotalSeconds)
            .Where(s => s >= 0).ToList();
        return xs.Count == 0 ? 0 : Math.Round(xs.Average(), 0);
    }

    private static double AiShare(IReadOnlyCollection<ConvTiming> t) =>
        t.Count == 0 ? 0 : Math.Round(t.Count(x => x.HasAi) * 100.0 / t.Count, 1);

    // ── Overview / KPI band ───────────────────────────────────────────────────
    public async Task<Result<OverviewResponse>> GetOverviewAsync(int? tenantId, string? role, int? uid, int? teamId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (tenantId is not { } tid) return Result.Failure<OverviewResponse>(AppErrors.NoTenantContext);
        if (from.Date > to.Date) return Result.Failure<OverviewResponse>(AppErrors.ReportInvalidRange);

        var baseConv = BaseConversations(tid, role, uid, teamId);

        var cur = await FetchTimingsAsync(baseConv, from, to, ct);
        var span = to - from;
        var prev = await FetchTimingsAsync(baseConv, from - span, from.AddSeconds(-1), ct);

        var active = await baseConv.CountAsync(c => c.Status != ConversationStatus.Resolved, ct);

        // Daily new-conversation series → shared sparkline for the tiles.
        var buckets = DayBuckets(from, to);
        var byDay = cur.GroupBy(x => x.CreatedAt.Date).ToDictionary(g => g.Key, g => g.Count());
        var spark = buckets.Select(d => (double)(byDay.TryGetValue(d, out var v) ? v : 0)).ToList();

        double curVol = cur.Count, prevVol = prev.Count;
        double curFr = AvgFirstResponse(cur), prevFr = AvgFirstResponse(prev);
        double curRes = AvgResolution(cur), prevRes = AvgResolution(prev);
        double curAi = AiShare(cur), prevAi = AiShare(prev);

        var kpis = new List<KpiDto>
        {
            new("volume", curVol, prevVol, Delta(curVol, prevVol), "number", spark),
            new("firstResponse", curFr, prevFr, Delta(curFr, prevFr), "duration", spark),
            new("resolution", curRes, prevRes, Delta(curRes, prevRes), "duration", spark),
            new("aiShare", curAi, prevAi, Delta(curAi, prevAi), "percent", spark),
            new("active", active, null, null, "number", spark),
        };

        return Result.Success(new OverviewResponse(kpis));
    }

    // ── Conversation volume (per-day inbound vs outbound messages) ────────────
    public async Task<Result<ConversationVolumeResponse>> GetConversationVolumeAsync(int? tenantId, string? role, int? uid, int? teamId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (tenantId is not { } tid) return Result.Failure<ConversationVolumeResponse>(AppErrors.NoTenantContext);
        if (from.Date > to.Date) return Result.Failure<ConversationVolumeResponse>(AppErrors.ReportInvalidRange);

        var msgQuery = db.Messages.AsNoTracking()
            .Where(m => m.Conversation.TenantId == tid && m.Timestamp >= from && m.Timestamp <= to);
        if (IsAgent(role)) msgQuery = msgQuery.Where(m => m.Conversation.AssignedUserId == uid);
        if (teamId is { } tm) msgQuery = msgQuery.Where(m => m.Conversation.GroupId == tm);

        var raw = await msgQuery
            .Select(m => new { m.Timestamp, m.SenderType })
            .ToListAsync(ct);

        var inboundByDay = raw.Where(x => x.SenderType == SenderType.Customer)
            .GroupBy(x => x.Timestamp.Date).ToDictionary(g => g.Key, g => g.Count());
        var outboundByDay = raw.Where(x => x.SenderType == SenderType.Agent || x.SenderType == SenderType.AI)
            .GroupBy(x => x.Timestamp.Date).ToDictionary(g => g.Key, g => g.Count());

        var points = DayBuckets(from, to).Select(d => new VolumePointDto(
            d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            inboundByDay.TryGetValue(d, out var i) ? i : 0,
            outboundByDay.TryGetValue(d, out var o) ? o : 0)).ToList();

        return Result.Success(new ConversationVolumeResponse(points));
    }

    // ── Agent leaderboard ─────────────────────────────────────────────────────
    public async Task<Result<AgentReportResponse>> GetAgentPerformanceAsync(int? tenantId, string? role, int? uid, int? teamId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (tenantId is not { } tid) return Result.Failure<AgentReportResponse>(AppErrors.NoTenantContext);
        if (from.Date > to.Date) return Result.Failure<AgentReportResponse>(AppErrors.ReportInvalidRange);

        var baseConv = BaseConversations(tid, role, uid, teamId);

        // Conversations an agent handled in-range (assigned + active during the window), with timing.
        var handled = await baseConv
            .Where(c => c.AssignedUserId != null && c.LastMessageAt >= from && c.LastMessageAt <= to)
            .Select(c => new
            {
                UserId = c.AssignedUserId!.Value,
                c.CreatedAt,
                c.LastMessageAt,
                Resolved = c.Status == ConversationStatus.Resolved,
                FirstAgentAt = db.Messages
                    .Where(m => m.ConversationId == c.Id && m.SenderType == SenderType.Agent)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => (DateTime?)m.Timestamp)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        // Current active (unresolved) chats per agent — a "now" snapshot, not range-bound.
        var activeAssigned = await baseConv
            .Where(c => c.AssignedUserId != null && c.Status != ConversationStatus.Resolved)
            .Select(c => c.AssignedUserId!.Value)
            .ToListAsync(ct);
        var activeByAgent = activeAssigned.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

        var nameById = await db.Users.AsNoTracking().Where(u => u.TenantId == tid)
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var buckets = DayBuckets(from, to);
        var agents = handled.GroupBy(x => x.UserId).Select(g =>
        {
            var list = g.ToList();
            var frs = list.Where(x => x.FirstAgentAt is not null)
                .Select(x => (x.FirstAgentAt!.Value - x.CreatedAt).TotalSeconds)
                .Where(s => s >= 0).ToList();
            var avgFr = frs.Count == 0 ? 0 : Math.Round(frs.Average(), 0);
            var resRate = Math.Round(list.Count(x => x.Resolved) * 100.0 / list.Count, 1);
            var perDay = list.GroupBy(x => (x.LastMessageAt ?? x.CreatedAt).Date)
                .ToDictionary(gg => gg.Key, gg => gg.Count());
            var spark = buckets.Select(d => (double)(perDay.TryGetValue(d, out var v) ? v : 0)).ToList();

            return new AgentRowDto(
                g.Key,
                nameById.TryGetValue(g.Key, out var nm) ? nm : "—",
                list.Count,
                avgFr,
                resRate,
                activeByAgent.TryGetValue(g.Key, out var ac) ? ac : 0,
                spark);
        })
        .OrderByDescending(a => a.Handled)
        .ToList();

        return Result.Success(new AgentReportResponse(agents));
    }

    // ── Response & resolution (daily series + distribution + SLA) ─────────────
    public async Task<Result<ResponseTimesResponse>> GetResponseTimesAsync(int? tenantId, string? role, int? uid, int? teamId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (tenantId is not { } tid) return Result.Failure<ResponseTimesResponse>(AppErrors.NoTenantContext);
        if (from.Date > to.Date) return Result.Failure<ResponseTimesResponse>(AppErrors.ReportInvalidRange);

        var timings = await FetchTimingsAsync(BaseConversations(tid, role, uid, teamId), from, to, ct);

        var byDay = timings.GroupBy(x => x.CreatedAt.Date).ToDictionary(g => g.Key, g => g.ToList());
        var series = DayBuckets(from, to).Select(d =>
        {
            var day = byDay.TryGetValue(d, out var l) ? l : new List<ConvTiming>();
            return new ResponsePointDto(
                d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                AvgFirstResponse(day),
                AvgResolution(day));
        }).ToList();

        var frSecs = timings.Where(x => x.FirstAgentAt is not null)
            .Select(x => (x.FirstAgentAt!.Value - x.CreatedAt).TotalSeconds)
            .Where(s => s >= 0).ToList();

        var distribution = new List<ResponseBucketDto>
        {
            new("< 1 min", frSecs.Count(s => s < 60)),
            new("1–5 min", frSecs.Count(s => s >= 60 && s < 300)),
            new("5–15 min", frSecs.Count(s => s >= 300 && s < 900)),
            new("15–60 min", frSecs.Count(s => s >= 900 && s < 3600)),
            new("> 1 hour", frSecs.Count(s => s >= 3600)),
        };

        var slaPct = frSecs.Count == 0 ? 0 : Math.Round(frSecs.Count(s => s <= SlaSeconds) * 100.0 / frSecs.Count, 1);

        return Result.Success(new ResponseTimesResponse(series, distribution, slaPct));
    }

    // ── Assignment split (routing) ────────────────────────────────────────────
    public async Task<Result<RoutingStatsResponse>> GetRoutingStatsAsync(int? tenantId, string? role, int? uid, int? teamId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (tenantId is not { } tid) return Result.Failure<RoutingStatsResponse>(AppErrors.NoTenantContext);
        if (from.Date > to.Date) return Result.Failure<RoutingStatsResponse>(AppErrors.ReportInvalidRange);

        try
        {
            var convIds = BaseConversations(tid, role, uid, teamId).Select(c => c.Id);
            var methods = await db.Assignments.AsNoTracking()
                .Where(a => convIds.Contains(a.ConversationId) && a.AssignedAt >= from && a.AssignedAt <= to)
                .Select(a => a.Method)
                .ToListAsync(ct);

            var slices = new List<RoutingSliceDto>
            {
                new("manual", methods.Count(m => m == AssignmentMethod.Manual)),
                new("roundRobin", methods.Count(m => m == AssignmentMethod.RoundRobin)),
                new("ai", methods.Count(m => m == AssignmentMethod.AIAssisted)),
            };
            return Result.Success(new RoutingStatsResponse(slices));
        }
        catch
        {
            return Result.Success(new RoutingStatsResponse(new List<RoutingSliceDto>()));
        }
    }

    // ── Export (CSV / PDF) ────────────────────────────────────────────────────
    private sealed record ReportTable(string Title, string[] Headers, List<string[]> Rows);

    public async Task<Result<ReportFile>> ExportAsync(int? tenantId, string? role, int? uid, int? teamId, string reportKey, string format, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (tenantId is null) return Result.Failure<ReportFile>(AppErrors.NoTenantContext);
        if (from.Date > to.Date) return Result.Failure<ReportFile>(AppErrors.ReportInvalidRange);

        var key = (reportKey ?? string.Empty).Trim().ToLowerInvariant();
        var fmt = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "csv";

        ReportTable table;
        switch (key)
        {
            case "overview":
            {
                var r = await GetOverviewAsync(tenantId, role, uid, teamId, from, to, ct);
                if (r.IsFailure) return Result.Failure<ReportFile>(r.Error);
                table = new ReportTable("Overview", ["KPI", "Value", "Previous", "Delta %"],
                    r.Value.Kpis.Select(k => new[]
                    {
                        k.Key,
                        Num(k.Value),
                        Num(k.PreviousValue),
                        Num(k.DeltaPct),
                    }).ToList());
                break;
            }
            case "agents":
            {
                var r = await GetAgentPerformanceAsync(tenantId, role, uid, teamId, from, to, ct);
                if (r.IsFailure) return Result.Failure<ReportFile>(r.Error);
                table = new ReportTable("Agent performance",
                    ["User ID", "Name", "Handled", "Avg response (s)", "Resolution rate (%)", "Active chats"],
                    r.Value.Agents.Select(a => new[]
                    {
                        a.UserId.ToString(),
                        a.Name,
                        a.Handled.ToString(),
                        Num(a.AvgResponseSec),
                        Num(a.ResolutionRate),
                        a.ActiveChats.ToString(),
                    }).ToList());
                break;
            }
            case "response-times":
            {
                var r = await GetResponseTimesAsync(tenantId, role, uid, teamId, from, to, ct);
                if (r.IsFailure) return Result.Failure<ReportFile>(r.Error);
                var rows = r.Value.Series
                    .Select(s => new[] { s.Date, Num(s.FirstResponseSec), Num(s.ResolutionSec) })
                    .ToList();
                rows.Add(["Within SLA (%)", Num(r.Value.SlaPct), string.Empty]);
                table = new ReportTable("Response & resolution",
                    ["Date", "First response (s)", "Resolution (s)"], rows);
                break;
            }
            case "volume":
            {
                var r = await GetConversationVolumeAsync(tenantId, role, uid, teamId, from, to, ct);
                if (r.IsFailure) return Result.Failure<ReportFile>(r.Error);
                table = new ReportTable("Conversation volume", ["Date", "Inbound", "Outbound"],
                    r.Value.Points.Select(p => new[] { p.Date, Num(p.Inbound), Num(p.Outbound) }).ToList());
                break;
            }
            case "routing":
            {
                var r = await GetRoutingStatsAsync(tenantId, role, uid, teamId, from, to, ct);
                if (r.IsFailure) return Result.Failure<ReportFile>(r.Error);
                table = new ReportTable("Assignment split", ["Method", "Count"],
                    r.Value.Slices.Select(s => new[] { s.Method, s.Count.ToString() }).ToList());
                break;
            }
            default:
                return Result.Failure<ReportFile>(AppErrors.ReportUnknown);
        }

        var stamp = $"{from:yyyyMMdd}-{to:yyyyMMdd}";
        if (fmt == "pdf")
        {
            try
            {
                var pdf = BuildPdf(table, from, to);
                return Result.Success(new ReportFile(pdf, "application/pdf", $"{key}-{stamp}.pdf"));
            }
            catch
            {
                // Keep export resilient — fall back to CSV if PDF rendering fails for any reason.
                return Result.Success(new ReportFile(BuildCsv(table), "text/csv", $"{key}-{stamp}.csv"));
            }
        }

        return Result.Success(new ReportFile(BuildCsv(table), "text/csv", $"{key}-{stamp}.csv"));
    }

    private static string Num(double? v) =>
        v is null ? string.Empty : v.Value.ToString(CultureInfo.InvariantCulture);

    private static byte[] BuildCsv(ReportTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", table.Headers.Select(CsvEscape)));
        foreach (var row in table.Rows)
            sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
        // UTF-8 BOM so Excel opens Arabic/Unicode content correctly.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string CsvEscape(string? value)
    {
        var v = value ?? string.Empty;
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

    private static byte[] BuildPdf(ReportTable table, DateTime from, DateTime to)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"WaslX — {table.Title}").FontSize(16).SemiBold();
                    col.Item().Text($"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(10).Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        for (var i = 0; i < table.Headers.Length; i++)
                            cols.RelativeColumn();
                    });

                    foreach (var h in table.Headers)
                        t.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(h).SemiBold();

                    foreach (var row in table.Rows)
                        foreach (var cell in row)
                            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cell ?? string.Empty);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
