using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Permission : BaseEntity
    {
        public string Code { get; set; } = string.Empty;          // e.g. "conversation.reply"
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;      // grouping on the settings screen
        public PermissionTier Tier { get; set; } = PermissionTier.Configurable;
        // Scope permissions (e.g. conversation.view_scope) resolve to a value from ScopeOptions
        // instead of a simple on/off — the tenant grant stores the chosen ScopeValue.
        public bool IsScope { get; set; }
        public string? ScopeOptions { get; set; }                 // CSV, e.g. "assigned,team,all"
        public int SortOrder { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();
        public ICollection<TenantRolePermission> TenantGrants { get; set; } = new HashSet<TenantRolePermission>();
    }
}
