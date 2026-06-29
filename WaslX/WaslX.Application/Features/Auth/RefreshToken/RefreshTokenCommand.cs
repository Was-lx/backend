using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string Token, string RefreshToken) : ICommand<AuthResponse>;

public class RefreshTokenCommandHandler(IAuthService authService) : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        authService.RefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
