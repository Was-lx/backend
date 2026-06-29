using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.ResendConfirmationEmail;

public record ResendConfirmationEmailCommand(string Email) : ICommand;

public class ResendConfirmationEmailCommandHandler(IAuthService authService) : ICommandHandler<ResendConfirmationEmailCommand>
{
    public Task<Result> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken) =>
        authService.ResendConfirmationEmailAsync(request.Email);
}

public class ResendConfirmationEmailCommandValidator : AbstractValidator<ResendConfirmationEmailCommand>
{
    public ResendConfirmationEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
