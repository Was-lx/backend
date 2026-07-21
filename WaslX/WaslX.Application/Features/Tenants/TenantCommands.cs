using WaslX.Application.Abstractions.Mediator;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Application.Features.Platform.Dtos;
using WaslX.Application.Features.Tenants.Dtos;
using WaslX.Domain.Results;

namespace WaslX.Application.Features.Tenants;

// ── Self-serve sign-up (public) ──
public record SignUpCommand(SelfServeSignupInput Input) : ICommand<AuthResponse>;
public class SignUpCommandHandler(ITenantProvisioningService svc) : ICommandHandler<SignUpCommand, AuthResponse>
{
    public Task<Result<AuthResponse>> Handle(SignUpCommand request, CancellationToken cancellationToken) =>
        svc.CreateSelfServeAsync(request.Input, cancellationToken);
}

// ── SuperAdmin: provision tenant ──
public record CreateTenantCommand(SuperAdminCreateTenantInput Input) : ICommand<int>;
public class CreateTenantCommandHandler(ITenantProvisioningService svc) : ICommandHandler<CreateTenantCommand, int>
{
    public Task<Result<int>> Handle(CreateTenantCommand request, CancellationToken cancellationToken) =>
        svc.CreateBySuperAdminAsync(request.Input, cancellationToken);
}

public record GetTenantsQuery() : IQuery<IReadOnlyList<TenantSummaryResponse>>;
public class GetTenantsQueryHandler(ITenantService svc) : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantSummaryResponse>>
{
    public Task<Result<IReadOnlyList<TenantSummaryResponse>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken) =>
        svc.GetAllAsync(cancellationToken);
}

public record SetTenantStatusCommand(int TenantId, string Status, PlatformActor Actor) : ICommand;
public class SetTenantStatusCommandHandler(ITenantService svc) : ICommandHandler<SetTenantStatusCommand>
{
    public Task<Result> Handle(SetTenantStatusCommand request, CancellationToken cancellationToken) =>
        svc.SetStatusAsync(request.TenantId, request.Status, request.Actor, cancellationToken);
}

// ── SuperAdmin: tenant lifecycle (US-6.2) ──
public record GetTenantDetailQuery(int TenantId) : IQuery<TenantDetailResponse>;
public class GetTenantDetailQueryHandler(ITenantService svc) : IQueryHandler<GetTenantDetailQuery, TenantDetailResponse>
{
    public Task<Result<TenantDetailResponse>> Handle(GetTenantDetailQuery request, CancellationToken cancellationToken) =>
        svc.GetDetailAsync(request.TenantId, cancellationToken);
}

public record ConfigureTenantCommand(int TenantId, ConfigureTenantInput Input, PlatformActor Actor) : ICommand;
public class ConfigureTenantCommandHandler(ITenantService svc) : ICommandHandler<ConfigureTenantCommand>
{
    public Task<Result> Handle(ConfigureTenantCommand request, CancellationToken cancellationToken) =>
        svc.ConfigureAsync(request.TenantId, request.Input, request.Actor, cancellationToken);
}

public record SoftDeleteTenantCommand(int TenantId, PlatformActor Actor) : ICommand;
public class SoftDeleteTenantCommandHandler(ITenantService svc) : ICommandHandler<SoftDeleteTenantCommand>
{
    public Task<Result> Handle(SoftDeleteTenantCommand request, CancellationToken cancellationToken) =>
        svc.SoftDeleteAsync(request.TenantId, request.Actor, cancellationToken);
}

// ── Tenant: own profile & onboarding ──
public record GetMyTenantQuery(int TenantId) : IQuery<TenantProfileResponse>;
public class GetMyTenantQueryHandler(ITenantService svc) : IQueryHandler<GetMyTenantQuery, TenantProfileResponse>
{
    public Task<Result<TenantProfileResponse>> Handle(GetMyTenantQuery request, CancellationToken cancellationToken) =>
        svc.GetProfileAsync(request.TenantId, cancellationToken);
}

public record UpdateMyTenantCommand(int TenantId, UpdateTenantProfileInput Input) : ICommand;
public class UpdateMyTenantCommandHandler(ITenantService svc) : ICommandHandler<UpdateMyTenantCommand>
{
    public Task<Result> Handle(UpdateMyTenantCommand request, CancellationToken cancellationToken) =>
        svc.UpdateProfileAsync(request.TenantId, request.Input, cancellationToken);
}

public record UpdateOnboardingCommand(int TenantId, int Step, bool Completed) : ICommand;
public class UpdateOnboardingCommandHandler(ITenantService svc) : ICommandHandler<UpdateOnboardingCommand>
{
    public Task<Result> Handle(UpdateOnboardingCommand request, CancellationToken cancellationToken) =>
        svc.UpdateOnboardingAsync(request.TenantId, request.Step, request.Completed, cancellationToken);
}
