using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Tenant : BaseEntity
    {
        public Guid PlanId { get; set; }
        public Guid PlatformUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TenantStatus Status { get; set; }
        public BillingStatus BillingStatus { get; set; }

        public SubscriptionPlan Plan { get; set; } = null!;
        public PlatformUser PlatformUser { get; set; } = null!;
        public ICollection<Invoice> Invoices { get; set; } = new HashSet<Invoice>();
        public ICollection<FAQ> FAQs { get; set; } = new HashSet<FAQ>();
        public ICollection<Customer> Customers { get; set; } = new HashSet<Customer>();
        public ICollection<KnowledgeVector> KnowledgeVectors { get; set; } = new HashSet<KnowledgeVector>();
        public ICollection<WhatsAppAccount> WhatsAppAccounts { get; set; } = new HashSet<WhatsAppAccount>();
        public ICollection<Group> Groups { get; set; } = new HashSet<Group>();
        public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
        public ICollection<User> Users { get; set; } = new HashSet<User>();
        public ICollection<Conversation> Conversations { get; set; } = new HashSet<Conversation>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>();
    }
}