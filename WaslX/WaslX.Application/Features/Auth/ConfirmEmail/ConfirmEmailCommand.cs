using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Auth.ConfirmEmail;

public record ConfirmEmailCommand(string Email, string Otp) : ICommand<AuthResponse>;

public class ConfirmEmailCommandHandler(IAuthService authService) : ICommandHandler<ConfirmEmailCommand, AuthResponse>
{
    public Task<Result<AuthResponse>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken) =>
        authService.ConfirmEmailAsync(request.Email, request.Otp);
}

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Otp)
            .NotEmpty();
    }
}
