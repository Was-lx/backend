using FluentValidation;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Mediator;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Users.AssignRole;

public record AssignRoleCommand(string UserId, string Role, int? CallerTenantId) : ICommand;

public class AssignRoleCommandHandler(IAuthService authService) : ICommandHandler<AssignRoleCommand>
{
    public Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken) =>
        authService.AssignRoleAsync(request.UserId, request.Role, request.CallerTenantId);
}

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
    }
}
