namespace WaslX.Domain.Entities
{
    /// <summary>
    /// A single cell of one tenant's Roles × Permissions matrix. Every tenant starts
    /// with the seeded default matrix and its Admin can flip the configurable cells.
    /// Keyed by (TenantId, Role, PermissionId). Role is the Identity role name
    /// (Admin / Manager / Agent) so it lines up with how auth actually works.
    /// </summary>
    public class TenantRolePermission
    {
        public int TenantId { get; set; }
        public string Role { get; set; } = string.Empty;
        public int PermissionId { get; set; }

        public bool IsGranted { get; set; }
        public string? ScopeValue { get; set; }   // for scope permissions, e.g. "assigned" | "team" | "all"

        public Tenant Tenant { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
    }
}
