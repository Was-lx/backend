namespace WaslX.Infrastructure.Identity;

/// <summary>
/// Fixed identifiers for the roles seeded into the database. Static Ids and
/// concurrency stamps keep EF Core seeding deterministic across migrations.
/// </summary>
public static class DefaultRoles
{
    // Platform owner — operates the SaaS across all tenants.
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string SuperAdminRoleId = "1d4f0b3a-7b6e-4c9a-9d21-0a1b2c3d4e5f";
    public const string SuperAdminRoleConcurrencyStamp = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";

    // Tenant owner — full control of a single tenant.
    public const string Admin = nameof(Admin);
    public const string AdminRoleId = "2e5a1c4b-8c7f-4d0b-ae32-1b2c3d4e5f60";
    public const string AdminRoleConcurrencyStamp = "b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e";

    // Sees all conversations, assigns/reassigns, views team reports.
    public const string Manager = nameof(Manager);
    public const string ManagerRoleId = "3f6b2d5c-9d80-4e1c-bf43-2c3d4e5f6071";
    public const string ManagerRoleConcurrencyStamp = "c3d4e5f6-a7b8-4c9d-ae1f-2a3b4c5d6e7f";

    // Handles assigned conversations only. Default role for new users.
    public const string Agent = nameof(Agent);
    public const string AgentRoleId = "4a7c3e6d-ae91-4f2d-c054-3d4e5f607182";
    public const string AgentRoleConcurrencyStamp = "d4e5f6a7-b8c9-4d0e-bf2a-3b4c5d6e7f80";

    public static readonly IReadOnlyList<string> All =
        [SuperAdmin, Admin, Manager, Agent];
}
