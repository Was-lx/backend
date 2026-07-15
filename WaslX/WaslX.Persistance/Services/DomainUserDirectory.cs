using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Identity;
using WaslX.Domain.Entities;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Resolves the domain <see cref="User"/> that mirrors the authenticated Identity user, creating it
/// (and its domain role, since users.RoleId is a required FK) the first time it's needed — the same
/// find-or-create by tenant + email used by <see cref="NoteService"/> / <see cref="PresenceService"/>.
/// </summary>
internal sealed class DomainUserDirectory(ApplicationDbContext db) : IDomainUserDirectory
{
    public async Task<int> GetOrCreateDomainUserIdAsync(int tenantId, string email, string? displayName = null, CancellationToken cancellationToken = default)
    {
        var existing = await db.Users
            .Where(u => u.TenantId == tenantId && u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            return existing.Value;

        const string effectiveRole = "Agent";
        var roleId = await db.Set<Role>()
            .Where(r => r.Name == effectiveRole)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId is null)
        {
            var role = new Role { Name = effectiveRole, Description = effectiveRole };
            await db.Set<Role>().AddAsync(role, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            roleId = role.Id;
        }

        var user = new User
        {
            TenantId = tenantId,
            RoleId = roleId.Value,
            Name = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            Email = email,
            PasswordHash = string.Empty,
            Status = "Active"
        };
        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<bool> IsOwnerAsync(int tenantId, string email, CancellationToken cancellationToken = default) =>
        await db.Users
            .Where(u => u.TenantId == tenantId && u.Email == email)
            .Select(u => u.IsOwner)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task EnsureOwnerAsync(int tenantId, string email, string? displayName = null, CancellationToken cancellationToken = default)
    {
        // Find-or-create the domain row (reuses the same tenant + email link), then flag it as owner.
        var userId = await GetOrCreateDomainUserIdAsync(tenantId, email, displayName, cancellationToken);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.IsOwner)
            return;

        user.IsOwner = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
