using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WaslX.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }


        public DbSet<AgentPerformance> AgentPerformances { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationSummary> ConversationSummaries { get; set; }
        public DbSet<ConversationStageHistory> ConversationStageHistories { get; set; }
        public DbSet<ConversationTag> ConversationTags { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<InternalNote> InternalNotes { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<KnowledgeVector> KnowledgeVectors { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageClassification> MessageClassifications { get; set; }
        public DbSet<Escalation> Escalations { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PlatformUser> PlatformUsers { get; set; }
        public new DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<TenantRolePermission> TenantRolePermissions { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<RoutingDecision> RoutingDecisions { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TemplateReview> TemplateReviews { get; set; }
        public DbSet<TemplateReviewHistory> TemplateReviewHistories { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignRecipient> CampaignRecipients { get; set; }
        public DbSet<CampaignTag> CampaignTags { get; set; }
        public new DbSet<User> Users { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<WhatsAppAccount> WhatsAppAccounts { get; set; }
        public DbSet<WhatsAppWebhookLog> WhatsAppWebhookLogs { get; set; }

        // ── Sprint 4: screening ──
        public DbSet<TenantEscalationSettings> TenantEscalationSettings { get; set; }

        // ── Sprint 3: channels, distribution & working hours ──
        public DbSet<Channel> Channels { get; set; }
        public DbSet<ChannelWhatsAppAccount> ChannelWhatsAppAccounts { get; set; }
        public DbSet<AgentChannelAccess> AgentChannelAccesses { get; set; }
        public DbSet<AgentWhatsAppDistribution> AgentWhatsAppDistributions { get; set; }
        public DbSet<CompanyWorkingDay> CompanyWorkingDays { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftDay> ShiftDays { get; set; }
        public DbSet<AgentShift> AgentShifts { get; set; }
    }
}

