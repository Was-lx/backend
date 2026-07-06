using WaslX.Application.Features.Profile.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Profile;

public interface IProfileService
{
    Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken cancellationToken = default);
}
