using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Tag : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Tenant Tenant { get; set; } = null!;
        public ICollection<ConversationTag> ConversationTags { get; set; } = new HashSet<ConversationTag>();
    }
}