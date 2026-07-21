using System.Threading;
using System.Threading.Tasks;
using WaslX.Application.Features.Escalation.Models;

namespace WaslX.Application.Abstractions.AI
{
    public interface IEscalationTargetScoringService
    {
        Task<EscalationScoringResult> ScoreAsync(EscalationScoringInput input, CancellationToken cancellationToken = default);
    }
}
