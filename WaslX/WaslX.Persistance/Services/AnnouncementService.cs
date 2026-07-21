using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

/// <summary>
/// Platform-wide announcements (US-6.10b). Create as a draft, then publish to broadcast: publishing sets
/// <c>PublishedAt</c>/<c>IsActive</c> and pushes an in-app notification (over SignalR, via
/// <see cref="INotificationService"/>) to the owner/Admin of every targeted tenant. <c>Audience</c> is one
/// of AllTenants | Plan | SpecificTenants; the target id set is stored as a JSON array in
/// <c>TargetTenantIds</c> (tenant ids for SpecificTenants, plan ids for Plan). Deliberately CROSS-TENANT —
/// the only caller is the SuperAdmin console. Every mutation is written to the platform audit trail.
/// </summary>
internal sealed class AnnouncementService(
    ApplicationDbContext db,
    INotificationService notifications,
    IPlatformAuditService audit) : IAnnouncementService
{
    private static readonly string[] AllowedAudiences = ["AllTenants", "Plan", "SpecificTenants"];

    public async Task<Result<IReadOnlyList<AnnouncementResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Announcements.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var list = rows.Select(Map).ToList();
        return Result.Success<IReadOnlyList<AnnouncementResponse>>(list);
    }

    public async Task<Result<AnnouncementResponse>> CreateAsync(CreateAnnouncementInput input, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var title = (input.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<AnnouncementResponse>(AppErrors.AnnouncementTitleRequired);

        var body = (input.Body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body))
            return Result.Failure<AnnouncementResponse>(AppErrors.AnnouncementBodyRequired);

        var audience = NormalizeAudience(input.Audience);

        var announcement = new Announcement
        {
            Title = title,
            Body = body,
            Severity = string.IsNullOrWhiteSpace(input.Severity) ? "Info" : input.Severity.Trim(),
            Audience = audience,
            TargetTenantIds = SerializeTargets(audience, input.TargetIds),
            ExpiresAt = input.ExpiresAt,
            IsActive = true,
            PublishedAt = null,
            CreatedByPlatformUserId = actor.ActorId ?? string.Empty
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "AnnouncementCreated", "Announcement", announcement.Id.ToString(),
            null, $"title='{title}'; audience={audience}", actor.Ip, cancellationToken);

        return Result.Success(Map(announcement));
    }

    public async Task<Result<AnnouncementResponse>> PublishAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (announcement is null)
            return Result.Failure<AnnouncementResponse>(AppErrors.AnnouncementNotFound);

        if (announcement.PublishedAt is not null)
            return Result.Failure<AnnouncementResponse>(AppErrors.AnnouncementAlreadyPublished);

        announcement.PublishedAt = DateTime.UtcNow;
        announcement.IsActive = true;
        announcement.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Broadcast: notify the owner/Admin of every targeted tenant (persisted + pushed over SignalR).
        var recipients = await ResolveRecipientsAsync(announcement, cancellationToken);
        foreach (var (tenantId, userId) in recipients)
        {
            await notifications.CreateAsync(tenantId, userId, "announcement", announcement.Title, announcement.Body,
                "Announcement", announcement.Id, cancellationToken);
        }

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "AnnouncementPublished", "Announcement", announcement.Id.ToString(),
            null, $"title='{announcement.Title}'; recipients={recipients.Count}", actor.Ip, cancellationToken);

        return Result.Success(Map(announcement));
    }

    public async Task<Result> DeactivateAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default)
    {
        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (announcement is null)
            return Result.Failure(AppErrors.AnnouncementNotFound);

        announcement.IsActive = false;
        announcement.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(actor.ActorId, actor.ActorEmail, "AnnouncementDeactivated", "Announcement", announcement.Id.ToString(),
            null, $"title='{announcement.Title}'", actor.Ip, cancellationToken);

        return Result.Success();
    }

    /// <summary>Resolve the (tenantId, domainUserId) pairs to notify: the owner/Admin of each targeted tenant.</summary>
    private async Task<IReadOnlyList<(int TenantId, int UserId)>> ResolveRecipientsAsync(Announcement announcement, CancellationToken cancellationToken)
    {
        var targetIds = ParseTargets(announcement.TargetTenantIds);

        // Which tenants are in scope for this announcement.
        var tenantsQuery = db.Tenants.AsNoTracking().AsQueryable();
        tenantsQuery = announcement.Audience switch
        {
            "SpecificTenants" => tenantsQuery.Where(t => targetIds.Contains(t.Id)),
            "Plan" => tenantsQuery.Where(t => targetIds.Contains(t.PlanId)),
            _ => tenantsQuery // AllTenants
        };
        var tenantIds = await tenantsQuery.Select(t => t.Id).ToListAsync(cancellationToken);
        if (tenantIds.Count == 0)
            return [];

        // The owner/Admin domain users of those tenants receive the notification.
        var recipients = await db.Users.AsNoTracking()
            .Where(u => tenantIds.Contains(u.TenantId) && (u.IsOwner || u.Role.Name == "Admin"))
            .Select(u => new { u.TenantId, u.Id })
            .ToListAsync(cancellationToken);

        return recipients.Select(r => (r.TenantId, r.Id)).ToList();
    }

    private static string NormalizeAudience(string? audience)
    {
        var value = (audience ?? string.Empty).Trim();
        return AllowedAudiences.FirstOrDefault(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase))
            ?? "AllTenants";
    }

    private static string? SerializeTargets(string audience, IReadOnlyList<int>? targetIds)
    {
        if (audience == "AllTenants" || targetIds is null || targetIds.Count == 0)
            return null;
        return JsonSerializer.Serialize(targetIds.Distinct().ToArray());
    }

    private static List<int> ParseTargets(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static AnnouncementResponse Map(Announcement a) =>
        new(a.Id, a.Title, a.Body, a.Severity, a.Audience, ParseTargets(a.TargetTenantIds),
            a.PublishedAt, a.ExpiresAt, a.IsActive, a.CreatedByPlatformUserId, a.CreatedAt);
}
