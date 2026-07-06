using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Users.SetUserStatus;

public record SetUserStatusCommand(string UserId, bool IsDisabled, int? CallerTenantId) : ICommand;

public class SetUserStatusCommandHandler(IAuthService authService) : ICommandHandler<SetUserStatusCommand>
{
    public Task<Result> Handle(SetUserStatusCommand request, CancellationToken cancellationToken) =>
        authService.SetUserStatusAsync(request.UserId, request.IsDisabled, request.CallerTenantId);
}
