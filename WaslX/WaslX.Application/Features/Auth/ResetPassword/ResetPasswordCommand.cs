using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Common;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand;

public class ResetPasswordCommandHandler(IAuthService authService) : ICommandHandler<ResetPasswordCommand>
{
    public Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
        authService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Matches(RegexPatterns.Password)
            .WithMessage("Password should be at least 8 characters and contain lowercase, uppercase and a non-alphanumeric character.");
    }
}
