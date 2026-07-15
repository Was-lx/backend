using System.Security.Claims;

namespace WaslX.Api.Extensions;

public static class UserExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.GivenName);
    }

    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email);
    }

    public static int? GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("tenantId");
        return int.TryParse(value, out var tenantId) ? tenantId : null;
    }

    // Domain User.Id (int, 'users' table) — distinct from the Identity GUID subject (NameIdentifier).
    // Used to scope conversations by Conversation.AssignedUserId and the Sprint-3 join tables.
    public static int? GetDomainUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("duid");
        return int.TryParse(value, out var domainUserId) ? domainUserId : null;
    }

    public static string? GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role);
    }
}
