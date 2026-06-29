using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Common;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.ChangePassword;

// UserId is populated by the controller from the authenticated principal.
public record ChangePasswordCommand(string UserId, string OldPassword, string NewPassword) : ICommand;

public class ChangePasswordCommandHandler(IAuthService authService) : ICommandHandler<ChangePasswordCommand>
{
    public Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
        authService.ChangePasswordAsync(request.UserId, request.OldPassword, request.NewPassword);
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Matches(RegexPatterns.Password)
            .WithMessage("Password should be at least 8 characters and contain lowercase, uppercase and a non-alphanumeric character.");

        RuleFor(x => x)
            .Must(x => x.OldPassword != x.NewPassword)
            .WithMessage("New password must be different from the old password.");
    }
}
