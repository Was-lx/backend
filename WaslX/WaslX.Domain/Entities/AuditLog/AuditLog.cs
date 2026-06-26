using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class AuditLog : BaseEntity
    {
        public int TenantId { get; set; }
        public int? ActorUserId { get; set; }
        public AuditAction Action { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Details { get; set; } = string.Empty;

        public Tenant Tenant { get; set; } = null!;
        public User? ActorUser { get; set; }
    }
}
