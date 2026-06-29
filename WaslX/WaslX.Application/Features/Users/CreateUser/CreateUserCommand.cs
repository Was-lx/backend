using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Users.CreateUser;

// TenantId is populated by the controller from the authenticated admin's claims.
public record CreateUserCommand(
    string Email,
    string FullName,
    string Role,
    int? TenantId,
    string? PhoneNumber) : ICommand<string>;

public class CreateUserCommandHandler(IAuthService authService) : ICommandHandler<CreateUserCommand, string>
{
    public Task<Result<string>> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        authService.CreateUserAsync(request.Email, request.FullName, request.Role, request.TenantId, request.PhoneNumber, cancellationToken);
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .Length(3, 100);

        RuleFor(x => x.Role)
            .NotEmpty();
    }
}
