using WaslX.Application.Features.Escalation.Screening;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Screening
{
    public interface IEscalationModeService
    {
        Task<Result<EscalationModeSettings>> GetSettingsAsync(int tenantId, CancellationToken cancellationToken = default);
        Task<Result<EscalationModeSettings>> UpdateSettingsAsync(int tenantId, int actorUserId, string mode, CancellationToken cancellationToken = default);
    }
}
