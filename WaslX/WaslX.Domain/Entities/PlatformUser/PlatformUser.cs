using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Entities.Base;
using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    public class PlatformUser : Account
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<Tenant> Tenants { get; set; } = new HashSet<Tenant>();
    }
}
