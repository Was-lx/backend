using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Platform-wide announcements (US-6.10b — Platform Owner console). Create as a draft, then publish to
/// broadcast: publishing sets <c>PublishedAt</c> and pushes an in-app notification (over SignalR) to the
/// admin/owner of every targeted tenant. Cross-tenant — only reached through the SuperAdmin console
/// (<c>[Authorize(Roles = "SuperAdmin")]</c>). Every mutation is written to the platform audit trail.
/// </summary>
public interface IAnnouncementService
{
    Task<Result<IReadOnlyList<AnnouncementResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AnnouncementResponse>> CreateAsync(CreateAnnouncementInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<AnnouncementResponse>> PublishAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(int id, PlatformActor actor, CancellationToken cancellationToken = default);
}
