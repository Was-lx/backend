using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Group : BaseEntity
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Tenant Tenant { get; set; } = null!;
        public ICollection<Stage> Stages { get; set; } = new HashSet<Stage>();
        public ICollection<UserGroup> UserGroups { get; set; } = new HashSet<UserGroup>();
        public ICollection<Conversation> Conversations { get; set; } = new HashSet<Conversation>();
    }
}
