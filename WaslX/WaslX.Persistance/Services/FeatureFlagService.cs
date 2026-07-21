using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Feature-flag management + resolution (US-6.7). A null-tenant row is the GLOBAL default for a key;
/// per-tenant rows override it. Deliberately CROSS-TENANT — the only caller is the SuperAdmin console
/// guarded by <c>[Authorize(Roles = "SuperAdmin")]</c>. The (Key, TenantId) pair is unique, so
/// <see cref="UpsertAsync"/> matches on it. Every mutation is written to the platform audit trail.
/// </summary>
internal sealed class FeatureFlagService(
    ApplicationDbContext db,
    IPlatformAuditService audit) : IFeatureFlagService
{
    public async Task<Result<IReadOnlyList<FeatureFlagResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var flags = await db.FeatureFlags.AsNoTracking()
            .OrderBy(f => f.Key).ThenBy(f => f.TenantId == null ? 0 : 1).ThenBy(f => f.TenantId)
            .Select(f => new FeatureFlagResponse(f.Id, f.Key, f.DisplayName, f.Description, f.TenantId, f.IsEnabled, f.CreatedAt))
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<FeatureFlagResponse>>(flags);
    }

    public async Task<Result<FeatureFlagResponse>> UpsertAsync(UpsertFeatureFlagInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var key = (input.Key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key)) return Result.Failure<FeatureFlagResponse>(AppErrors.FeatureFlagKeyRequired);

        if (input.TenantId is { } tenantId && !await db.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken))
            return Result.Failure<FeatureFlagResponse>(AppErrors.TenantNotFound);

        // Match on the unique (Key, TenantId) pair: update in place, else create.
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key && f.TenantId == input.TenantId, cancellationToken);
        var isNew = flag is null;
        if (flag is null)
        {
            flag = new FeatureFlag { Key = key, TenantId = input.TenantId };
            db.FeatureFlags.Add(flag);
        }

        flag.DisplayName = (input.DisplayName ?? string.Empty).Trim();
        flag.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        flag.IsEnabled = input.IsEnabled;
        if (!isNew) flag.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, isNew ? "FeatureFlagCreated" : "FeatureFlagUpdated", "FeatureFlag", flag.Id.ToString(),
            flag.TenantId, $"key='{flag.Key}'; enabled={flag.IsEnabled}; scope={(flag.TenantId == null ? "global" : "tenant")}", actor.Ip, cancellationToken);

        return Result.Success(Map(flag));
    }

    public async Task<Result<FeatureFlagResponse>> ToggleAsync(int id, ToggleFeatureFlagInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (flag is null) return Result.Failure<FeatureFlagResponse>(AppErrors.FeatureFlagNotFound);

        flag.IsEnabled = input.IsEnabled;
        flag.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "FeatureFlagToggled", "FeatureFlag", flag.Id.ToString(),
            flag.TenantId, $"key='{flag.Key}'; enabled={flag.IsEnabled}", actor.Ip, cancellationToken);

        return Result.Success(Map(flag));
    }

    public async Task<Result> DeleteAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (flag is null) return Result.Failure(AppErrors.FeatureFlagNotFound);

        db.FeatureFlags.Remove(flag);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "FeatureFlagDeleted", "FeatureFlag", id.ToString(),
            flag.TenantId, $"key='{flag.Key}'; scope={(flag.TenantId == null ? "global" : "tenant")}", actor.Ip, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ResolvedFeatureFlagResponse>> ResolveAsync(string key, int? tenantId, CancellationToken cancellationToken = default)
    {
        var trimmed = (key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return Result.Failure<ResolvedFeatureFlagResponse>(AppErrors.FeatureFlagKeyRequired);

        // A per-tenant row wins over the global (null-tenant) row.
        if (tenantId is { } tid)
        {
            var tenantRow = await db.FeatureFlags.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Key == trimmed && f.TenantId == tid, cancellationToken);
            if (tenantRow is not null)
                return Result.Success(new ResolvedFeatureFlagResponse(trimmed, tenantId, tenantRow.IsEnabled, "tenant"));
        }

        var globalRow = await db.FeatureFlags.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == trimmed && f.TenantId == null, cancellationToken);
        if (globalRow is not null)
            return Result.Success(new ResolvedFeatureFlagResponse(trimmed, tenantId, globalRow.IsEnabled, "global"));

        // No row at all → the flag is off by default.
        return Result.Success(new ResolvedFeatureFlagResponse(trimmed, tenantId, false, "default"));
    }

    private static FeatureFlagResponse Map(FeatureFlag f) =>
        new(f.Id, f.Key, f.DisplayName, f.Description, f.TenantId, f.IsEnabled, f.CreatedAt);
}
