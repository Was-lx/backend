using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class FAQ : BaseEntity
    {
        public int TenantId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public Language Language { get; set; }
        public bool IsActive { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
