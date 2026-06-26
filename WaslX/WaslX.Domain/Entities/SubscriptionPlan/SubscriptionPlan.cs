using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int MaxAgents { get; set; }
        public int MaxNumbers { get; set; }
        public int MonthlyMsgQuota { get; set; }
        public int MonthlyAiQuota { get; set; }
        public decimal Price { get; set; }
        public BillingCycle BillingCycle { get; set; }

        public ICollection<Tenant> Tenants { get; set; } = new HashSet<Tenant>();
    }
}