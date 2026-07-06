using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.Logout;

// UserId is populated by the controller from the authenticated user's claims.
public record LogoutCommand(string UserId) : ICommand;

public class LogoutCommandHandler(IAuthService authService) : ICommandHandler<LogoutCommand>
{
    public Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        authService.LogoutAsync(request.UserId, cancellationToken);
}

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
