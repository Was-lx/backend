using WaslX.Application.Features.Platform.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Platform;

/// <summary>
/// Global platform policy management (US-6.7 — Platform Owner console) over <c>PlatformSetting</c> rows
/// (data retention, API rate limit, default routing mode). Global — no tenant scope, guarded by the
/// SuperAdmin console. Every change is written to the platform audit trail.
/// </summary>
public interface IPlatformPolicyService
{
    Task<Result<PlatformPolicyResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<PlatformPolicyResponse>> SetAsync(SetPlatformPolicyInput input, PlatformActor actor, CancellationToken cancellationToken = default);
}
