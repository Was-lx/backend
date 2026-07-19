using WaslX.Application.Features.Escalation.Models;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AutoEscalation;

public interface IConversationEscalationService
{
    Task<Result<EscalationResult>> EscalateAsync(
        EscalationInput input, CancellationToken cancellationToken = default);
}
