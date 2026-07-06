using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Api.Extensions;

/// <summary>
/// Idempotent first-run seed so a brand-new database is usable out of the box:
/// the 3 subscription plans (needed before anyone can sign up) and one ready-to-use
/// demo workspace + Admin login. Every step is guarded by an existence check, so it
/// runs safely on every startup and no-ops once the data is present.
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoAdminEmail = "admin@demo.waslx.com";
    public const string DemoAdminPassword = "Admin@123";
    public const string DemoTenantName = "Demo Company";

    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var permissions = sp.GetRequiredService<IPermissionService>();

        await SeedPlansAsync(db);
        await SeedDemoWorkspaceAsync(db, userManager, permissions, app.Logger);
    }

    /// <summary>The public pricing plans. Sign-up can't work without at least one plan.</summary>
    private static async Task SeedPlansAsync(ApplicationDbContext db)
    {
        if (await db.SubscriptionPlans.AnyAsync())
            return;

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Code = "starter", Name = "Starter", Tagline = "For small teams getting organized on WhatsApp.",
                Price = 29m, PriceYearly = 24m, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 3, MaxNumbers = 1, MsgQuota = 5000, AiQuota = 500, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = false, SortOrder = 1,
                Features = new() { "Up to 3 agents", "1 WhatsApp number", "Shared team inbox", "Manual & round-robin routing", "Tags & filters", "Basic reporting" }
            },
            new SubscriptionPlan
            {
                Code = "growth", Name = "Growth", Tagline = "The full AI pipeline for scaling teams.",
                Price = 99m, PriceYearly = 82m, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 15, MaxNumbers = 2, MsgQuota = 20000, AiQuota = 3000, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = false, SortOrder = 2,
                Features = new() { "Up to 15 agents", "2 WhatsApp numbers", "AI routing + RAG memory", "AI reply suggestions", "Campaigns & broadcasts", "Advanced analytics", "Groups & stage handoff" }
            },
            new SubscriptionPlan
            {
                Code = "enterprise", Name = "Enterprise", Tagline = "Advanced control, scale and support.",
                Price = 299m, PriceYearly = 249m, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 9999, MaxNumbers = 99, MsgQuota = 1000000, AiQuota = 500000, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = false, SortOrder = 3,
                Features = new() { "Unlimited agents", "Multiple WhatsApp numbers", "Custom AI limits", "SSO & advanced RBAC", "Priority support & SLA", "Dedicated onboarding" }
            });

        await db.SaveChangesAsync();
    }

    /// <summary>One ready demo tenant on the Growth plan + its Admin (known password, so it's a working login).</summary>
    private static async Task SeedDemoWorkspaceAsync(
        ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPermissionService permissions, ILogger logger)
    {
        // If the demo admin already exists, self-heal: force its password back to the
        // documented one, unlock it, and confirm its email — so the demo login always
        // works regardless of leftover state from prior testing.
        if (await userManager.FindByEmailAsync(DemoAdminEmail) is { } existing)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            await userManager.ResetPasswordAsync(existing, token, DemoAdminPassword);
            await userManager.SetLockoutEndDateAsync(existing, null);
            await userManager.ResetAccessFailedCountAsync(existing);
            if (!existing.EmailConfirmed)
            {
                existing.EmailConfirmed = true;
                await userManager.UpdateAsync(existing);
            }
            return;
        }

        var growth = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == "growth");
        if (growth is null)
            return; // no plans → nothing to attach the tenant to

        var tenant = new Tenant
        {
            Name = DemoTenantName,
            PlanId = growth.Id,
            Status = TenantStatus.Active,
            BillingStatus = BillingStatus.Active,
            Industry = "Technology",
            CustomerType = CustomerType.B2B,
            SelectedBillingCycle = BillingCycle.Monthly,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            OnboardingStep = 3,
            OnboardingCompleted = true,
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var admin = new ApplicationUser
        {
            Email = DemoAdminEmail,
            UserName = DemoAdminEmail,
            FullName = "Demo Admin",
            TenantId = tenant.Id,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, DemoAdminPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("Demo admin seed failed: {errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, DefaultRoles.Admin);
        await permissions.SeedDefaultMatrixAsync(tenant.Id);

        logger.LogInformation("Seeded demo workspace '{tenant}' with Admin login {email} / {password}",
            DemoTenantName, DemoAdminEmail, DemoAdminPassword);
    }
}
