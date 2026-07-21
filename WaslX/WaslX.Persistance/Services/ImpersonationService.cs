using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Authentication;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Audited SuperAdmin impersonation (US-6.8). Starting a session selects the target tenant's owner (or
/// an Admin) as the impersonated identity, records an <see cref="ImpersonationSession"/>, then mints a
/// SHORT-LIVED tenant JWT bounded to the session window and tagged with an <c>imp</c> claim = the session
/// id. Deliberately CROSS-TENANT — the only caller is the SuperAdmin console guarded by
/// <c>[Authorize(Roles = "SuperAdmin")]</c>. Both start and end are written to the platform audit trail.
/// </summary>
internal sealed class ImpersonationService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwt,
    IDomainUserDirectory domainUsers,
    IPlatformAuditService audit) : IImpersonationService
{
    // A single impersonation window is capped at 30 minutes.
    private const int SessionMinutes = 30;

    public async Task<Result<StartImpersonationResponse>> StartAsync(StartImpersonationInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var reason = (input.Reason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<StartImpersonationResponse>(AppErrors.ImpersonationReasonRequired);

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == input.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure<StartImpersonationResponse>(AppErrors.TenantNotFound);

        // Pick the impersonated identity: prefer the workspace owner, else any (enabled) Admin.
        var admins = (await userManager.GetUsersInRoleAsync(DefaultRoles.Admin))
            .Where(u => u.TenantId == tenant.Id && !u.IsDisabled && !string.IsNullOrEmpty(u.Email))
            .ToList();

        ApplicationUser? target = null;
        foreach (var candidate in admins)
        {
            if (await domainUsers.IsOwnerAsync(tenant.Id, candidate.Email!, cancellationToken))
            {
                target = candidate;
                break;
            }
        }
        target ??= admins.FirstOrDefault();
        if (target is null)
            return Result.Failure<StartImpersonationResponse>(AppErrors.TenantHasNoAdmin);

        var roles = await userManager.GetRolesAsync(target);
        var domainUserId = await domainUsers.GetOrCreateDomainUserIdAsync(tenant.Id, target.Email!, target.FullName, cancellationToken);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(SessionMinutes);

        // Record the session first so it has an id to embed in the token's "imp" claim.
        var session = new ImpersonationSession
        {
            ActorPlatformUserId = actor.ActorId ?? string.Empty,
            ActorEmail = actor.ActorEmail ?? string.Empty,
            TenantId = tenant.Id,
            TargetUserId = domainUserId,
            Reason = reason,
            StartedAt = now,
            ExpiresAt = expiresAt,
            Status = "Active"
        };
        db.ImpersonationSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = jwt.GenerateImpersonationToken(
            target.Id, target.Email!, target.FullName, roles, tenant.Id, domainUserId, expiresAt, session.Id.ToString());

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, AuditAction.Impersonated.ToString(), "Tenant", tenant.Id.ToString(),
            tenant.Id, $"started; sessionId={session.Id}; targetUserId={domainUserId}; reason='{reason}'", actor.Ip, cancellationToken);

        var response = new StartImpersonationResponse(
            accessToken.Token,
            expiresAt,
            new ImpersonatedTenantSummary(tenant.Id, tenant.Name, tenant.Status.ToString()),
            Map(session));

        return Result.Success(response);
    }

    public async Task<Result<ImpersonationSessionResponse>> EndAsync(int sessionId, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var session = await db.ImpersonationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
            return Result.Failure<ImpersonationSessionResponse>(AppErrors.ImpersonationSessionNotFound);

        if (session.Status != "Active")
            return Result.Failure<ImpersonationSessionResponse>(AppErrors.ImpersonationAlreadyEnded);

        session.EndedAt = DateTime.UtcNow;
        session.Status = "Ended";
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, AuditAction.Impersonated.ToString(), "Tenant", session.TenantId.ToString(),
            session.TenantId, $"ended; sessionId={session.Id}", actor.Ip, cancellationToken);

        return Result.Success(Map(session));
    }

    public async Task<Result<IReadOnlyList<ImpersonationSessionResponse>>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sessions = await db.ImpersonationSessions.AsNoTracking()
            .Where(s => s.Status == "Active" && s.ExpiresAt > now)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new ImpersonationSessionResponse(
                s.Id, s.ActorPlatformUserId, s.ActorEmail, s.TenantId, s.TargetUserId,
                s.Reason, s.StartedAt, s.ExpiresAt, s.EndedAt, s.Status))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ImpersonationSessionResponse>>(sessions);
    }

    private static ImpersonationSessionResponse Map(ImpersonationSession s) =>
        new(s.Id, s.ActorPlatformUserId, s.ActorEmail, s.TenantId, s.TargetUserId,
            s.Reason, s.StartedAt, s.ExpiresAt, s.EndedAt, s.Status);
}
