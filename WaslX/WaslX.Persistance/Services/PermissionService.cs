using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Features.Permissions.Dtos;
using WaslX.Domain.Authorization;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Resolves and edits per-tenant permissions. The hard tier rules (AdminOnly / ManagerPlus)
/// are re-applied on every read and write, so a bad row in the DB can never grant a locked
/// capability — the stored matrix only ever governs the *configurable* cells.
/// </summary>
internal sealed class PermissionService(ApplicationDbContext db) : IPermissionService
{
    private const string SuperAdmin = "SuperAdmin";

    public async Task<EffectivePermissionsResponse> GetEffectiveAsync(int? tenantId, string role, CancellationToken cancellationToken = default)
    {
        var granted = new List<string>();
        var scopes = new Dictionary<string, string>();

        // SuperAdmin operates the platform, and a tenant Admin always has every capability.
        var isFull = role is SuperAdmin or PermissionCatalog.RoleAdmin;

        var rows = (!isFull && tenantId is not null)
            ? await db.TenantRolePermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Role == role)
                .ToDictionaryAsync(x => x.PermissionId, x => new { x.IsGranted, x.ScopeValue }, cancellationToken)
            : new();

        foreach (var def in PermissionCatalog.All)
        {
            bool effective;
            if (isFull) effective = true;
            else if (def.Tier == PermissionTier.AdminOnly) effective = false;
            else if (def.Tier == PermissionTier.ManagerPlus && role == PermissionCatalog.RoleAgent) effective = false;
            else effective = rows.TryGetValue(def.Id, out var row) ? row.IsGranted : PermissionCatalog.DefaultFor(def, role).granted;

            if (!effective) continue;
            granted.Add(def.Code);

            if (def.IsScope)
            {
                string? scope = rows.TryGetValue(def.Id, out var r) && r.ScopeValue is not null
                    ? r.ScopeValue
                    : PermissionCatalog.DefaultFor(def, isFull ? PermissionCatalog.RoleAdmin : role).scope;
                if (scope is not null) scopes[def.Code] = scope;
            }
        }

        return new EffectivePermissionsResponse(granted, scopes);
    }

    public async Task<bool> HasAsync(int? tenantId, string role, string permissionCode, CancellationToken cancellationToken = default)
    {
        var effective = await GetEffectiveAsync(tenantId, role, cancellationToken);
        return effective.Granted.Contains(permissionCode);
    }

    public async Task<Result<PermissionMatrixResponse>> GetTenantMatrixAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var grants = await db.TenantRolePermissions.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var byKey = grants.ToDictionary(g => (g.Role, g.PermissionId));

        var categories = PermissionCatalog.All
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Min(p => p.Sort))
            .Select(cat => new PermissionCategoryDto(
                cat.Key,
                cat.OrderBy(p => p.Sort).Select(def => new PermissionRowDto(
                    def.Code, def.Description, def.Tier.ToString(), def.IsScope, def.ScopeOptions, def.Sort,
                    PermissionCatalog.Roles.ToDictionary(r => r, r => BuildCell(def, r, byKey))
                )).ToList()))
            .ToList();

        return Result.Success(new PermissionMatrixResponse(PermissionCatalog.Roles.ToList(), categories));
    }

    private static PermissionCellDto BuildCell(PermissionDef def, string role, IReadOnlyDictionary<(string, int), TenantRolePermission> byKey)
    {
        // Admin is always full & not editable; locked cells are shown but disabled.
        bool lockedForRole = PermissionCatalog.IsLockedFor(def, role);
        bool locked = lockedForRole || role == PermissionCatalog.RoleAdmin;

        bool granted;
        string? scope;
        if (role == PermissionCatalog.RoleAdmin)
        {
            granted = true;
            scope = def.IsScope ? def.AdminScope : null;
        }
        else if (byKey.TryGetValue((role, def.Id), out var row))
        {
            granted = row.IsGranted;
            scope = row.ScopeValue;
        }
        else
        {
            (granted, scope) = PermissionCatalog.DefaultFor(def, role);
        }

        if (lockedForRole) granted = false; // enforce the lock in the displayed value too
        return new PermissionCellDto(granted, scope, locked);
    }

    public async Task<Result> UpdateTenantMatrixAsync(int tenantId, IReadOnlyList<PermissionUpdateItem> changes, CancellationToken cancellationToken = default)
    {
        var permByCode = PermissionCatalog.All.ToDictionary(p => p.Code);

        foreach (var change in changes)
        {
            if (!permByCode.TryGetValue(change.Code, out var def)) continue; // unknown permission
            if (change.Role == PermissionCatalog.RoleAdmin) continue;         // Admin is not editable
            if (!PermissionCatalog.Roles.Contains(change.Role)) continue;     // unknown role
            if (PermissionCatalog.IsLockedFor(def, change.Role)) continue;    // locked — silently reject

            var row = await db.TenantRolePermissions
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Role == change.Role && x.PermissionId == def.Id, cancellationToken);

            if (row is null)
            {
                row = new TenantRolePermission { TenantId = tenantId, Role = change.Role, PermissionId = def.Id };
                db.TenantRolePermissions.Add(row);
            }

            row.IsGranted = change.Granted;
            if (def.IsScope) row.ScopeValue = change.Scope;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task SeedDefaultMatrixAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        if (await db.TenantRolePermissions.AnyAsync(x => x.TenantId == tenantId, cancellationToken))
            return;

        var rows = new List<TenantRolePermission>();
        foreach (var def in PermissionCatalog.All)
            foreach (var role in PermissionCatalog.Roles)
            {
                var (granted, scope) = PermissionCatalog.DefaultFor(def, role);
                rows.Add(new TenantRolePermission { TenantId = tenantId, Role = role, PermissionId = def.Id, IsGranted = granted, ScopeValue = scope });
            }

        db.TenantRolePermissions.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);
    }
}
