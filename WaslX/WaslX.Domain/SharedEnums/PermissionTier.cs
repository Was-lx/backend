namespace WaslX.Domain.SharedEnums
{
    /// <summary>
    /// How much freedom a tenant Admin has over a permission on the Roles & Permissions screen.
    /// The tier is enforced server-side regardless of any stored grant (defence in depth).
    /// </summary>
    public enum PermissionTier
    {
        /// <summary>Every role's toggle is editable by the Admin.</summary>
        Configurable,

        /// <summary>Never grantable to Agent; Admin & Manager may toggle it.</summary>
        ManagerPlus,

        /// <summary>Reserved for Admin — never grantable to Manager or Agent, and not configurable.</summary>
        AdminOnly
    }
}
