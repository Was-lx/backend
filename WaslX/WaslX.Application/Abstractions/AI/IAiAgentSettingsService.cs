using WaslX.Application.Features.AiAgent.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.AI;

public interface IAiAgentSettingsService
{
    Task<Result<AiAgentSettingsResponse>> GetSettingsAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Result<AiAgentSettingsResponse>> UpdateSettingsAsync(int tenantId, UpdateAiAgentSettingsRequest request, int? updatedByUserId, CancellationToken cancellationToken = default);
}
