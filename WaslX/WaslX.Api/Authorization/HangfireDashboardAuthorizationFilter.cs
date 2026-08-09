using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using WaslX.Infrastructure.Settings;

namespace WaslX.Api.Authorization;

/// <summary>
/// Gates /hangfire with HTTP Basic Auth. The dashboard is a plain server-rendered page reached by
/// direct browser navigation, so it can't rely on the SPA's JWT — that token only ever travels via
/// a manually-attached Authorization header on XHR/fetch calls, never on a full page load. Without
/// this filter, Hangfire's default is effectively open to anyone who can reach the route.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter(IOptions<HangfireSettings> options) : IDashboardAuthorizationFilter
{
    private readonly HangfireSettings _settings = options.Value;

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Never fail open: no configured credentials means no access, not "anyone may in".
        if (string.IsNullOrEmpty(_settings.DashboardUsername) || string.IsNullOrEmpty(_settings.DashboardPassword))
        {
            httpContext.Response.StatusCode = 403;
            return false;
        }

        var header = httpContext.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase) || !TryValidate(header["Basic ".Length..]))
        {
            httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
            httpContext.Response.StatusCode = 401;
            return false;
        }

        return true;
    }

    private bool TryValidate(string base64Credentials)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex < 0)
                return false;

            var user = decoded[..separatorIndex];
            var pass = decoded[(separatorIndex + 1)..];

            return FixedTimeEquals(user, _settings.DashboardUsername) && FixedTimeEquals(pass, _settings.DashboardPassword);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
