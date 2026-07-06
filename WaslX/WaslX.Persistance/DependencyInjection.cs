using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;
using WaslX.Persistance.Repos;
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
            services.AddScoped<WaslX.Application.Abstractions.Permissions.IPermissionService, WaslX.Persistance.Services.PermissionService>();
            services.AddScoped<WaslX.Application.Abstractions.Billing.ISubscriptionPlanService, WaslX.Persistance.Services.SubscriptionPlanService>();
            services.AddScoped<WaslX.Application.Abstractions.Billing.ISubscriptionService, WaslX.Persistance.Services.SubscriptionService>();
            services.AddScoped<WaslX.Application.Abstractions.Tenants.ITenantProvisioningService, WaslX.Persistance.Services.TenantProvisioningService>();
            services.AddScoped<WaslX.Application.Abstractions.Tenants.ITenantService, WaslX.Persistance.Services.TenantService>();
            services.AddScoped<WaslX.Application.Abstractions.Profile.IProfileService, WaslX.Persistance.Services.ProfileService>();

            return services;
        }

    }
}
