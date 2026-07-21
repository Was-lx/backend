using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;

namespace WaslX.Api.Extensions;

/// <summary>
/// Idempotent first-run seed so a brand-new database is usable out of the box:
/// the 4 subscription plans (needed before anyone can sign up) and one ready-to-use
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
        var domainUsers = sp.GetRequiredService<IDomainUserDirectory>();

        await SeedPlansAsync(db);
        await SeedDemoWorkspaceAsync(db, userManager, permissions, domainUsers, app.Logger);
        await HealTenantOwnersAsync(db, userManager, domainUsers, app.Logger);
    }

    /// <summary>The public pricing plans. Sign-up can't work without at least one plan.</summary>
    private static async Task SeedPlansAsync(ApplicationDbContext db)
    {
        if (await db.SubscriptionPlans.AnyAsync())
            return;

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Code = "starter", Name = "Starter", Tagline = "For small teams handling WhatsApp support manually.",
                Price = 30m, PriceYearly = 25m, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 2, MaxNumbers = 1, MsgQuota = 1000, AiQuota = 1000, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = false, SortOrder = 1,
                Features = new() { "Up to 1,000 conversations / month", "Up to 2 agents", "AI reply suggestions (1–3 per message)", "Manual conversation assignment", "Shared database", "Email support" }
            },
            new SubscriptionPlan
            {
                Code = "growth", Name = "Growth", Tagline = "The full AI pipeline for scaling support teams.",
                Price = 138m, PriceYearly = 115m, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 5, MaxNumbers = 2, MsgQuota = 3000, AiQuota = 3000, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = false, SortOrder = 2,
                Features = new() { "Up to 3,000 conversations / month", "Up to 5 agents", "Full AI pipeline: RAG memory, routing & classification", "Conversation summaries on handoff", "Auto-escalation for urgent / VIP / angry conversations", "Off-hours acknowledgment & FAQ auto-resolve", "Email + live chat support" }
            },
            new SubscriptionPlan
            {
                Code = "business", Name = "Business", Tagline = "Everything in Growth, tuned for larger teams.",
                Price = 340m, PriceYearly = 282m, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 15, MaxNumbers = 5, MsgQuota = 8000, AiQuota = 8000, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = false, SortOrder = 3,
                Features = new() { "Up to 8,000 conversations / month", "Up to 15 agents", "Everything in Growth", "Agent scoring & smart selection", "Priority support" }
            },
            new SubscriptionPlan
            {
                Code = "enterprise", Name = "Enterprise", Tagline = "Custom scale, isolation and support.",
                Price = 0m, PriceYearly = null, BillingCycle = BillingCycle.Monthly,
                MaxAgents = 9999, MaxNumbers = 99, MsgQuota = 1000000, AiQuota = 1000000, TrialDays = 7,
                IsActive = true, IsPublic = true, IsCustom = true, SortOrder = 4,
                Features = new() { "Unlimited conversations & agents", "Dedicated database per tenant", "Dedicated account manager", "Custom integrations & SLA" }
            });

        await db.SaveChangesAsync();
    }

    /// <summary>One ready demo tenant on the Growth plan + its Admin (known password, so it's a working login).</summary>
    private static async Task SeedDemoWorkspaceAsync(
        ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPermissionService permissions, IDomainUserDirectory domainUsers, ILogger logger)
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

        // The first Admin is the workspace owner (locked role, can't be disabled).
        await domainUsers.EnsureOwnerAsync(tenant.Id, DemoAdminEmail, admin.FullName);

        logger.LogInformation("Seeded demo workspace '{tenant}' with Admin login {email} / {password}",
            DemoTenantName, DemoAdminEmail, DemoAdminPassword);
    }

    /// <summary>
    /// One-time, idempotent backfill: for any tenant that has no owner yet, mark the oldest Admin as
    /// the workspace owner. Runs safely on every startup — it no-ops once every tenant has an owner.
    /// </summary>
    private static async Task HealTenantOwnersAsync(
        ApplicationDbContext db, UserManager<ApplicationUser> userManager, IDomainUserDirectory domainUsers, ILogger logger)
    {
        // Tenants that already have an owner need no healing.
        var tenantsWithOwner = await db.Users
            .Where(u => u.IsOwner)
            .Select(u => u.TenantId)
            .Distinct()
            .ToListAsync();
        var owned = tenantsWithOwner.ToHashSet();

        // Group every Admin (Identity) by tenant; a tenant's owner is its oldest Admin.
        var admins = await userManager.GetUsersInRoleAsync(DefaultRoles.Admin);
        var byTenant = admins
            .Where(u => u.TenantId is not null && !string.IsNullOrEmpty(u.Email))
            .GroupBy(u => u.TenantId!.Value);

        foreach (var group in byTenant)
        {
            var tenantId = group.Key;
            if (owned.Contains(tenantId))
                continue;

            var emails = group.Select(u => u.Email!).ToList();

            // Prefer the oldest existing domain user row among the tenant's admins.
            var existingOldest = await db.Users
                .Where(u => u.TenantId == tenantId && emails.Contains(u.Email))
                .OrderBy(u => u.CreatedAt)
                .ThenBy(u => u.Id)
                .FirstOrDefaultAsync();

            if (existingOldest is not null)
            {
                existingOldest.IsOwner = true;
                await db.SaveChangesAsync();
                logger.LogInformation("Owner backfill: marked domain user {userId} as owner of tenant {tenantId}", existingOldest.Id, tenantId);
            }
            else
            {
                // No domain row yet: create one for a deterministic admin and flag it.
                var email = emails.OrderBy(e => e, StringComparer.OrdinalIgnoreCase).First();
                var admin = group.First(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                await domainUsers.EnsureOwnerAsync(tenantId, email, admin.FullName);
                logger.LogInformation("Owner backfill: created + marked owner {email} for tenant {tenantId}", email, tenantId);
            }
        }
    }
}
