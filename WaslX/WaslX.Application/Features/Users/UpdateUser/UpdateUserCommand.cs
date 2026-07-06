using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Users.UpdateUser;

public record UpdateUserCommand(
    string UserId,
    string FullName,
    string? PhoneNumber,
    int? CallerTenantId) : ICommand;

public class UpdateUserCommandHandler(IAuthService authService) : ICommandHandler<UpdateUserCommand>
{
    public Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken) =>
        authService.UpdateUserAsync(request.UserId, request.FullName, request.PhoneNumber, request.CallerTenantId, cancellationToken);
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .Length(3, 100);
    }
}
