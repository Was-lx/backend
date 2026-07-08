using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Abstractions.Profile;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;
using WaslX.Persistance.Repos;
using WaslX.Persistance.Services;
using WaslX.Persistance.UnitOfWorks;

namespace WaslX.Persistance
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning))
                ;
            });

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Account lockout: lock for 15 min after 5 consecutive failed sign-ins.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.SignIn.RequireConfirmedEmail = true;
            })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

            // Platform / billing / RBAC services (need direct DbContext access).
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IProfileService, ProfileService>();

            // WhatsApp (needs direct DbContext for multi-entity conversation/message writes).
            services.AddScoped<IWhatsAppService, WhatsAppService>();
            services.AddScoped<IWhatsAppWebhookProcessor, WhatsAppWebhookProcessor>();

            return services;
        }

    }
}
