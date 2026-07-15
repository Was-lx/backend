using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaslX.Application.Abstractions.AgentAccess;
using WaslX.Application.Abstractions.Assignments;
using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Abstractions.Channels;
using WaslX.Application.Abstractions.ConversationStages;
using WaslX.Application.Abstractions.Distribution;
using WaslX.Application.Abstractions.Groups;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Maintenance;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Abstractions.Presence;
using WaslX.Application.Abstractions.Profile;
using WaslX.Application.Abstractions.Tags;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Abstractions.WorkingHours;
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
            services.AddScoped<IWhatsAppTemplateService, WhatsAppTemplateService>();
            services.AddScoped<IConversationWindowService, ConversationWindowService>();

            // Shared inbox (conversation list / history / reply / notes).
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<INoteService, NoteService>();

            // Bridges the Identity user (GUID) to the domain User (int) for JWT inbox scoping.
            services.AddScoped<IDomainUserDirectory, DomainUserDirectory>();

            // Channels, distribution & working hours (Sprint 3).
            services.AddScoped<IChannelService, ChannelService>();
            services.AddScoped<IWorkingHoursService, WorkingHoursService>();
            services.AddScoped<IAgentAccessService, AgentAccessService>();

            // Groups / teams, stages & membership (Sprint 3).
            services.AddScoped<IGroupService, GroupService>();

            // Conversation stage pipeline + cross-team handoff (Sprint 3).
            services.AddScoped<IConversationStageService, ConversationStageService>();

            // Tags (Sprint 3).
            services.AddScoped<ITagService, TagService>();

            // Manual assignment / reassignment + unassigned queue (Sprint 3).
            services.AddScoped<IAssignmentService, AssignmentService>();

            // Agent presence & break state (drives Round Robin distribution).
            services.AddScoped<IPresenceService, PresenceService>();

            // Auto-distribution engine (Round Robin / working-hours routing + offline reassignment).
            services.AddScoped<IDistributionService, DistributionService>();

            // Recurring maintenance jobs (auto-resolve stale conversations + reassign offline agents).
            services.AddScoped<IMaintenanceJobs, MaintenanceJobs>();

            return services;
        }

    }
}
