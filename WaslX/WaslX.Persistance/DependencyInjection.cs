using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaslX.Application.Abstractions.AgentAccess;
using WaslX.Application.Abstractions.Assignments;
using WaslX.Application.Abstractions.Audit;
using WaslX.Application.Abstractions.Billing;
using WaslX.Application.Abstractions.Campaigns;
using WaslX.Application.Abstractions.Channels;
using WaslX.Application.Abstractions.ConversationStages;
using WaslX.Application.Abstractions.Customers;
using WaslX.Application.Abstractions.Distribution;
using WaslX.Application.Abstractions.Groups;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Abstractions.Inbox;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Application.Abstractions.Maintenance;
using WaslX.Application.Abstractions.Notifications;
using WaslX.Application.Abstractions.Permissions;
using WaslX.Application.Abstractions.Platform;
using WaslX.Application.Abstractions.Presence;
using WaslX.Application.Abstractions.Profile;
using WaslX.Application.Abstractions.Reporting;
using WaslX.Application.Abstractions.Tags;
using WaslX.Application.Abstractions.Tenants;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Abstractions.WorkingHours;
using WaslX.Domain.Contracts.Infrastructure;
using WaslX.Infrastructure.Identity;
using WaslX.Persistance.Data;
using WaslX.Persistance.Repos;
using WaslX.Persistance.Services;
using WaslX.Persistance.Services.Knowledge;
using WaslX.Persistance.Services.Knowledge.Sources;
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

            // AI conversation summary — cached one-line + on-demand full summary.
            services.AddScoped<IConversationSummaryService, ConversationSummaryService>();

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

            // Reporting & analytics dashboards + export (Sprint 5 — FR-RPT, read-only).
            services.AddScoped<IReportingService, ReportingService>();

            // Campaigns / broadcasts + Hangfire send engine (Sprint 5 — FR-CMP).
            services.AddScoped<ICampaignService, CampaignService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ICampaignSendJob, CampaignSendJob>();

            // In-app notifications (Sprint 5 — FR-NOTIF).
            services.AddScoped<INotificationService, NotificationService>();

            // Immutable tenant audit trail (Sprint 5 — FR-AUDIT / US-5.7).
            services.AddScoped<IAuditService, AuditService>();

            // Global, immutable platform audit trail (Sprint 6 — Platform Owner console).
            services.AddScoped<IPlatformAuditService, PlatformAuditService>();

            // SuperAdmin console management (Sprint 6 — US-6.1 super-admin users, US-6.3 billing/invoicing).
            services.AddScoped<ISuperAdminUserService, SuperAdminUserService>();
            services.AddScoped<ISuperAdminBillingService, SuperAdminBillingService>();

            // Platform monitoring & configuration (Sprint 6 — US-6.4 usage, US-6.5 AI cost,
            // US-6.6 credentials/secrets, US-6.7 feature flags + global policy). All cross-tenant.
            services.AddScoped<IPlatformMetricsService, PlatformMetricsService>();
            services.AddScoped<IAiCostService, AiCostService>();
            services.AddScoped<IPlatformCredentialService, PlatformCredentialService>();
            services.AddScoped<IFeatureFlagService, FeatureFlagService>();
            services.AddScoped<IPlatformPolicyService, PlatformPolicyService>();

            // Audited impersonation + platform announcements (Sprint 6 — US-6.8, US-6.10b). Cross-tenant.
            services.AddScoped<IImpersonationService, ImpersonationService>();
            services.AddScoped<IAnnouncementService, AnnouncementService>();

            // Manual assignment / reassignment + unassigned queue (Sprint 3).
            services.AddScoped<IAssignmentService, AssignmentService>();

            // Agent presence & break state (drives Round Robin distribution).
            services.AddScoped<IPresenceService, PresenceService>();

            // Auto-distribution engine (Round Robin / working-hours routing + offline reassignment).
            services.AddScoped<IDistributionService, DistributionService>();

            // Recurring maintenance jobs (auto-resolve stale conversations + reassign offline agents).
            services.AddScoped<IMaintenanceJobs, MaintenanceJobs>();

            // RAG knowledge ingestion orchestrator (needs direct DbContext access).
            services.AddScoped<IKnowledgeIngestionPipeline, KnowledgeIngestionPipeline>();
            services.AddScoped<IKnowledgeService, KnowledgeService>();
            services.AddScoped<IKnowledgeSource, FaqKnowledgeSource>();

            // AI Agent
            services.AddScoped<WaslX.Application.Abstractions.AI.IAiAgentSettingsService, WaslX.Persistance.Services.AiAgent.AiAgentSettingsService>();
            services.AddScoped<WaslX.Application.Abstractions.AI.IAiAgentReplyService, WaslX.Persistance.Services.AiAgent.AiAgentReplyService>();

            return services;
        }

    }
}
