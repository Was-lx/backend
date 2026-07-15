using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.Agents.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Presence;

/// <summary>
/// Tracks an agent's live presence (online/offline) and break state. Presence drives the
/// Round Robin distribution eligibility rules, so it is resolved against the domain
/// <c>User</c> row (found-or-created by tenant + email, mirroring the authenticated Identity user).
/// </summary>
public interface IPresenceService
{
    /// <summary>Marks the agent online and stamps LastSeenAt. Safe no-op when tenant/email is missing.</summary>
    Task SetOnlineAsync(int? tenantId, string? email, CancellationToken cancellationToken = default);

    /// <summary>Marks the agent offline and stamps LastSeenAt. Safe no-op when tenant/email is missing.</summary>
    Task SetOfflineAsync(int? tenantId, string? email, CancellationToken cancellationToken = default);

    /// <summary>Toggles the agent's break state.</summary>
    Task<Result<AvailabilityResponse>> SetBreakAsync(int? tenantId, string? email, bool onBreak, CancellationToken cancellationToken = default);

    /// <summary>Returns the caller's current online/break/last-seen state.</summary>
    Task<Result<AvailabilityResponse>> GetMyAvailabilityAsync(int? tenantId, string? email, CancellationToken cancellationToken = default);
}
