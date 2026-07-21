using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Global platform policy over <see cref="PlatformSetting"/> rows (US-6.7): data-retention days, API
/// rate-limit per minute, default routing mode. Deliberately GLOBAL — no tenant scope, only reached
/// through the SuperAdmin console guarded by <c>[Authorize(Roles = "SuperAdmin")]</c>. Each key is
/// upserted by <see cref="PlatformSetting.Key"/>. Every change is written to the platform audit trail.
/// </summary>
internal sealed class PlatformPolicyService(
    ApplicationDbContext db,
    IPlatformAuditService audit) : IPlatformPolicyService
{
    // Well-known policy keys + sensible defaults when the row is absent.
    private const string RetentionKey = "retention.days";
    private const string RateLimitKey = "ratelimit.perMinute";
    private const string RoutingKey = "routing.defaultMode";
    private const int DefaultRetentionDays = 90;
    private const int DefaultRateLimitPerMinute = 60;
    private const string DefaultRoutingMode = "RoundRobin";

    public async Task<Result<PlatformPolicyResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.PlatformSettings.AsNoTracking()
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);

        return Result.Success(Build(settings));
    }

    public async Task<Result<PlatformPolicyResponse>> SetAsync(SetPlatformPolicyInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var changes = new List<string>();

        if (input.RetentionDays is { } retention)
        {
            await UpsertAsync(RetentionKey, retention.ToString(), "int", "Data retention window in days", cancellationToken);
            changes.Add($"{RetentionKey}={retention}");
        }
        if (input.RateLimitPerMinute is { } rate)
        {
            await UpsertAsync(RateLimitKey, rate.ToString(), "int", "API rate limit per minute", cancellationToken);
            changes.Add($"{RateLimitKey}={rate}");
        }
        if (!string.IsNullOrWhiteSpace(input.RoutingDefaultMode))
        {
            var mode = input.RoutingDefaultMode.Trim();
            await UpsertAsync(RoutingKey, mode, "string", "Default conversation routing mode", cancellationToken);
            changes.Add($"{RoutingKey}={mode}");
        }

        if (changes.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "PlatformPolicyUpdated", "PlatformSetting", string.Empty,
                null, string.Join("; ", changes), actor.Ip, cancellationToken);
        }

        var settings = await db.PlatformSettings.AsNoTracking().OrderBy(s => s.Key).ToListAsync(cancellationToken);
        return Result.Success(Build(settings));
    }

    // Upsert a single setting row by key (tracked; caller commits once).
    private async Task UpsertAsync(string key, string value, string valueType, string description, CancellationToken cancellationToken)
    {
        var setting = await db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null)
        {
            db.PlatformSettings.Add(new PlatformSetting { Key = key, Value = value, ValueType = valueType, Description = description });
        }
        else
        {
            setting.Value = value;
            setting.ValueType = valueType;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static PlatformPolicyResponse Build(IReadOnlyList<PlatformSetting> settings)
    {
        var byKey = settings.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);

        var retention = byKey.TryGetValue(RetentionKey, out var r) && int.TryParse(r, out var ri) ? ri : DefaultRetentionDays;
        var rate = byKey.TryGetValue(RateLimitKey, out var rl) && int.TryParse(rl, out var rli) ? rli : DefaultRateLimitPerMinute;
        var routing = byKey.TryGetValue(RoutingKey, out var rm) && !string.IsNullOrWhiteSpace(rm) ? rm : DefaultRoutingMode;

        var items = settings
            .Select(s => new PlatformSettingItem(s.Key, s.Value, s.ValueType, s.Description))
            .ToList();

        return new PlatformPolicyResponse(retention, rate, routing, items);
    }
}
