using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class SubscriptionPlan : BaseEntity
    {
        public string Code { get; set; } = string.Empty;   // stable slug: starter / growth / enterprise
        public string Name { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public int MaxAgents { get; set; }
        public int MaxNumbers { get; set; }
        public int MsgQuota { get; set; }
        public int AiQuota { get; set; }
        public decimal Price { get; set; }         // monthly price (0 for custom/enterprise)
        public decimal? PriceYearly { get; set; }  // per-month equivalent when billed yearly
        public BillingCycle BillingCycle { get; set; }
        public int TrialDays { get; set; } = 7;
        public bool IsActive { get; set; } = true;   // usable / assignable
        public bool IsPublic { get; set; } = true;   // shown on the public pricing page
        public bool IsCustom { get; set; }           // "Contact sales" (Enterprise) — no self-serve checkout
        public int SortOrder { get; set; }

        // Display bullet list for the pricing/subscription UI (stored as JSON).
        public List<string> Features { get; set; } = new();

        public ICollection<Tenant> Tenants { get; set; } = new HashSet<Tenant>();
    }
}
