using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.RevokeRefreshToken;

public record RevokeRefreshTokenCommand(string Token, string RefreshToken) : ICommand;

public class RevokeRefreshTokenCommandHandler(IAuthService authService) : ICommandHandler<RevokeRefreshTokenCommand>
{
    public Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken) =>
        authService.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);
}

public class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
