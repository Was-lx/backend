using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.ForgetPassword;

public record ForgetPasswordCommand(string Email) : ICommand;

public class ForgetPasswordCommandHandler(IAuthService authService) : ICommandHandler<ForgetPasswordCommand>
{
    public Task<Result> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken) =>
        authService.SendResetPasswordCodeAsync(request.Email);
}

public class ForgetPasswordCommandValidator : AbstractValidator<ForgetPasswordCommand>
{
    public ForgetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
