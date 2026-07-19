using WaslX.Application.Features.Escalation.Screening;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Screening
{
    public interface IEscalationAssignmentService
    {
        Task<Result<EscalationRecommendation>> GetRecommendationAsync(int tenantId, int conversationId, CancellationToken cancellationToken = default);
        Task<Result<EscalationRecommendation>> ConfirmAsync(int tenantId, int actorUserId, int escalationId, int assigneeId, CancellationToken cancellationToken = default);
        Task<Result<EscalationRecommendation>> OverrideAsync(int tenantId, int actorUserId, int escalationId, int assigneeId, string reason, CancellationToken cancellationToken = default);
        Task<Result<EscalationRecommendation>> HandleScoringResultAsync(int tenantId, int escalationId, CancellationToken cancellationToken = default);
    }
}
