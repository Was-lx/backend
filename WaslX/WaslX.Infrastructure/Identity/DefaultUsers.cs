namespace WaslX.Infrastructure.Identity;

/// <summary>
/// Fixed identifiers for the seed SuperAdmin account so the platform is usable
/// immediately after applying migrations. Change the password after first login.
/// </summary>
public static class DefaultUsers
{
    public const string SuperAdminId = "0f9e8d7c-6b5a-4938-2716-0c1d2e3f4a5b";
    public const string SuperAdminEmail = "superadmin@waslx.com";
    public const string SuperAdminUserName = "superadmin@waslx.com";
    public const string SuperAdminFullName = "Platform Owner";
    public const string SuperAdminPassword = "SuperAdmin@123";
    public const string SuperAdminConcurrencyStamp = "e5f6a7b8-c9d0-4e1f-a2b3-4c5d6e7f8091";
    public const string SuperAdminSecurityStamp = "f6a7b8c9-d0e1-4f2a-b3c4-5d6e7f809102";
}
