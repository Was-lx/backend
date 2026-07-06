using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Features.Auth.Dtos;
using WaslX.Application.Features.Tenants.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed class TenantProvisioningService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService,
    IAuthService authService) : ITenantProvisioningService
{
    public async Task<Result<AuthResponse>> CreateSelfServeAsync(SelfServeSignupInput input, CancellationToken cancellationToken = default)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        if (await userManager.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return Result.Failure<AuthResponse>(UserErrors.DuplicatedEmail);

        var plan = await PickTrialPlanAsync(cancellationToken);
        if (plan is null)
            return Result.Failure<AuthResponse>(AppErrors.NoPlansConfigured);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new Tenant
        {
            Name = input.OrgName.Trim(),
            PlanId = plan.Id,
            Status = TenantStatus.Active,
            BillingStatus = BillingStatus.Trial,
            Website = Clean(input.Website),
            Industry = Clean(input.Industry),
            PhoneNumber = Clean(input.Phone),
            CustomerType = ParseCustomerType(input.CustomerType),
            TrialEndsAt = DateTime.UtcNow.AddDays(plan.TrialDays > 0 ? plan.TrialDays : 7),
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = input.FullName.Trim(),
            PhoneNumber = Clean(input.Phone),
            EmailConfirmed = true,            // self-serve trial: skip email verification to reach onboarding fast
            TenantId = tenant.Id,
        };

        var created = await userManager.CreateAsync(user, input.Password);
        if (!created.Succeeded)
        {
            await tx.RollbackAsync(cancellationToken);
            var e = created.Errors.First();
            return Result.Failure<AuthResponse>(new Error(e.Code, e.Description, 400));
        }

        await userManager.AddToRoleAsync(user, DefaultRoles.Admin);
        await permissionService.SeedDefaultMatrixAsync(tenant.Id, cancellationToken);

        await tx.CommitAsync(cancellationToken);

        // Log the new Admin straight in.
        return await authService.BuildSessionAsync(user.Id);
    }

    public async Task<Result<int>> CreateBySuperAdminAsync(SuperAdminCreateTenantInput input, CancellationToken cancellationToken = default)
    {
        var email = input.AdminEmail.Trim().ToLowerInvariant();
        if (await userManager.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return Result.Failure<int>(UserErrors.DuplicatedEmail);

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == input.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<int>(AppErrors.PlanNotFound);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new Tenant
        {
            Name = input.OrgName.Trim(),
            PlanId = plan.Id,
            Status = TenantStatus.Active,
            BillingStatus = input.StartTrial ? BillingStatus.Trial : BillingStatus.Active,
            Website = Clean(input.Website),
            Industry = Clean(input.Industry),
            PhoneNumber = Clean(input.Phone),
            CustomerType = ParseCustomerType(input.CustomerType),
            TrialEndsAt = input.StartTrial ? DateTime.UtcNow.AddDays(plan.TrialDays > 0 ? plan.TrialDays : 7) : null,
            CurrentPeriodEnd = input.StartTrial ? null : DateTime.UtcNow.AddMonths(1),
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        // Reuse the existing admin-user creation (emails a set-password code).
        var adminResult = await authService.CreateUserAsync(email, input.AdminFullName.Trim(), DefaultRoles.Admin, tenant.Id, Clean(input.Phone), cancellationToken);
        if (adminResult.IsFailure)
        {
            await tx.RollbackAsync(cancellationToken);
            return Result.Failure<int>(adminResult.Error);
        }

        await permissionService.SeedDefaultMatrixAsync(tenant.Id, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Result.Success(tenant.Id);
    }

    private async Task<SubscriptionPlan?> PickTrialPlanAsync(CancellationToken cancellationToken)
    {
        var plans = await db.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(cancellationToken);
        return plans.FirstOrDefault(p => p.Code == "growth")
            ?? plans.Where(p => p.IsPublic && !p.IsCustom).OrderBy(p => p.SortOrder).FirstOrDefault();
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static CustomerType ParseCustomerType(string v) => Enum.TryParse<CustomerType>(v, true, out var ct) ? ct : CustomerType.Unknown;
}
