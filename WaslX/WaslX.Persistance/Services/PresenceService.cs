using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Presence;
using WaslX.Application.Features.Agents.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Agent presence + break state. The presence flags live on the domain <see cref="User"/> row (the same
/// row Round Robin distribution reads), which is mirrored from the authenticated Identity user and
/// found-or-created by tenant + email exactly like <see cref="NoteService"/> / <see cref="ConversationService"/>.
/// </summary>
internal sealed class PresenceService(ApplicationDbContext db) : IPresenceService
{
    public async Task SetOnlineAsync(int? tenantId, string? email, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid || string.IsNullOrWhiteSpace(email))
            return;

        var user = await ResolveOrCreateUserAsync(tid, email, cancellationToken);
        user.IsOnline = true;
        user.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetOfflineAsync(int? tenantId, string? email, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid || string.IsNullOrWhiteSpace(email))
            return;

        var user = await ResolveOrCreateUserAsync(tid, email, cancellationToken);
        user.IsOnline = false;
        user.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<AvailabilityResponse>> SetBreakAsync(int? tenantId, string? email, bool onBreak, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<AvailabilityResponse>(AppErrors.NoTenantContext);
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<AvailabilityResponse>(AppErrors.UserContextNotResolved);

        var user = await ResolveOrCreateUserAsync(tid, email, cancellationToken);
        user.IsOnBreak = onBreak;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AvailabilityResponse(user.IsOnline, user.IsOnBreak, user.LastSeenAt));
    }

    public async Task<Result<AvailabilityResponse>> GetMyAvailabilityAsync(int? tenantId, string? email, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tid)
            return Result.Failure<AvailabilityResponse>(AppErrors.NoTenantContext);
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<AvailabilityResponse>(AppErrors.UserContextNotResolved);

        var existing = await db.Users.AsNoTracking()
            .Where(u => u.TenantId == tid && u.Email == email)
            .Select(u => new AvailabilityResponse(u.IsOnline, u.IsOnBreak, u.LastSeenAt))
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(existing ?? new AvailabilityResponse(false, false, null));
    }

    /// <summary>
    /// Returns the domain <see cref="User"/> that mirrors the authenticated Identity user, creating it
    /// (and its domain role, since users.RoleId is a required FK) the first time it's needed.
    /// </summary>
    private async Task<User> ResolveOrCreateUserAsync(int tenantId, string email, CancellationToken cancellationToken)
    {
        var existing = await db.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
        if (existing is not null)
            return existing;

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
            Name = email,
            Email = email,
            PasswordHash = string.Empty,
            Status = "Active"
        };
        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
