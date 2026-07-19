using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Tenant : BaseEntity
    {
        public int PlanId { get; set; }
        // Nullable: self-serve tenants (a business that signs up itself) are owned by
        // their own Admin user, not by a platform super-admin. Only tenants provisioned
        // from the SuperAdmin console carry a PlatformUserId.
        public int? PlatformUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TenantStatus Status { get; set; }
        public BillingStatus BillingStatus { get; set; }

        // ── Organization profile (collected at sign-up / by the super-admin) ──
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? PhoneNumber { get; set; }
        public CustomerType CustomerType { get; set; } = CustomerType.Unknown;

        // ── Trial & billing lifecycle ──
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public BillingCycle? SelectedBillingCycle { get; set; }   // chosen at subscribe (monthly | yearly)

        // ── Onboarding wizard progress (resumable) ──
        public int OnboardingStep { get; set; }
        public bool OnboardingCompleted { get; set; }

        public SubscriptionPlan Plan { get; set; } = null!;
        public PlatformUser? PlatformUser { get; set; }
        public ICollection<TenantRolePermission> RolePermissions { get; set; } = new HashSet<TenantRolePermission>();
        public ICollection<PaymentMethod> PaymentMethods { get; set; } = new HashSet<PaymentMethod>();
        public ICollection<Invoice> Invoices { get; set; } = new HashSet<Invoice>();
        public ICollection<FAQ> FAQs { get; set; } = new HashSet<FAQ>();
        public ICollection<Customer> Customers { get; set; } = new HashSet<Customer>();
        public ICollection<KnowledgeVector> KnowledgeVectors { get; set; } = new HashSet<KnowledgeVector>();
        public ICollection<KnowledgeDocument> KnowledgeDocuments { get; set; } = new HashSet<KnowledgeDocument>();
        public ICollection<WhatsAppAccount> WhatsAppAccounts { get; set; } = new HashSet<WhatsAppAccount>();
        public ICollection<Group> Groups { get; set; } = new HashSet<Group>();
        public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
        public ICollection<User> Users { get; set; } = new HashSet<User>();
        public ICollection<Conversation> Conversations { get; set; } = new HashSet<Conversation>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>();
        public ICollection<Campaign> Campaigns { get; set; } = new HashSet<Campaign>();

        // ── Sprint 3: workspace operational settings ──
        public string TimeZoneId { get; set; } = "Egypt Standard Time";
        public bool AutoResolveEnabled { get; set; } = true;
        public int AutoResolveHours { get; set; } = 24;
        public bool StickyReturningCustomer { get; set; } = true;
        public ICollection<Channel> Channels { get; set; } = new HashSet<Channel>();
        public ICollection<Shift> Shifts { get; set; } = new HashSet<Shift>();
        public ICollection<CompanyWorkingDay> CompanyWorkingDays { get; set; } = new HashSet<CompanyWorkingDay>();
    }
}
