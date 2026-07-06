using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Profile;
using WaslX.Application.Features.Profile.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Profile;

public record GetMeQuery(string UserId) : IQuery<MeResponse>;
public class GetMeQueryHandler(IProfileService svc) : IQueryHandler<GetMeQuery, MeResponse>
{
    public Task<Result<MeResponse>> Handle(GetMeQuery request, CancellationToken cancellationToken) =>
        svc.GetMeAsync(request.UserId, cancellationToken);
}
