using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Customer : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool VipFlag { get; set; }
        public CustomerTier Tier { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public ICollection<KnowledgeVector> KnowledgeVectors { get; set; } = new HashSet<KnowledgeVector>();
        public ICollection<Conversation> Conversations { get; set; } = new HashSet<Conversation>();
    }
}