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

    public static int? GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("tenantId");
        return int.TryParse(value, out var tenantId) ? tenantId : null;
    }
}
