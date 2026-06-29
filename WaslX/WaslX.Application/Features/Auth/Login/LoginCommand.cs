using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;

public class LoginCommandHandler(IAuthService authService) : ICommandHandler<LoginCommand, AuthResponse>
{
    public Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request.Email, request.Password, cancellationToken);
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
