using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Audited SuperAdmin impersonation (US-6.8 — Platform Owner console). Starting a session mints a
/// SHORT-LIVED tenant JWT (as the target workspace's admin/owner) bounded to the session window, and
/// records an <c>ImpersonationSession</c>. Cross-tenant — only reached through the SuperAdmin console
/// (<c>[Authorize(Roles = "SuperAdmin")]</c>). Both start and end are written to the platform audit trail.
/// </summary>
public interface IImpersonationService
{
    Task<Result<StartImpersonationResponse>> StartAsync(StartImpersonationInput input, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<ImpersonationSessionResponse>> EndAsync(int sessionId, PlatformActor actor, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ImpersonationSessionResponse>>> GetActiveAsync(CancellationToken cancellationToken = default);
}
