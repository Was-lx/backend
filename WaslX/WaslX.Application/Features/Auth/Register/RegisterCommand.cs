using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Common;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber) : ICommand;

public class RegisterCommandHandler(IAuthService authService) : ICommandHandler<RegisterCommand>
{
    public Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        authService.RegisterAsync(request.Email, request.Password, request.FullName, request.PhoneNumber, cancellationToken);
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .Matches(RegexPatterns.Password)
            .WithMessage("Password should be at least 8 characters and contain lowercase, uppercase and a non-alphanumeric character.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .Length(3, 100);
    }
}
