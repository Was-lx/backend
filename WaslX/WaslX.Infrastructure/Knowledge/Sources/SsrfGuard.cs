using System.Net;
using System.Net.Sockets;

namespace WaslX.Infrastructure.Knowledge.Sources;

/// <summary>
/// Blocks the website knowledge source from ever fetching a private/internal address — otherwise a
/// tenant could point "Add website" at http://169.254.169.254 (cloud metadata) or an internal
/// service and exfiltrate it back to themselves through the extracted text.
/// </summary>
internal static class SsrfGuard
{
    public static bool IsUrlSafe(string url, out string reason)
    {
        reason = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            reason = "Invalid URL";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            reason = "Only http/https URLs are allowed";
            return false;
        }

        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(uri.Host);
        }
        catch
        {
            reason = "Could not resolve host";
            return false;
        }

        if (addresses.Length == 0 || addresses.Any(IsPrivateOrInternal))
        {
            reason = "URL resolves to a private or internal address";
            return false;
        }

        return true;
    }

    private static bool IsPrivateOrInternal(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] switch
            {
                0 => true,
                10 => true,
                127 => true,
                169 when b[1] == 254 => true, // link-local, incl. cloud metadata 169.254.169.254
                172 when b[1] is >= 16 and <= 31 => true,
                192 when b[1] == 168 => true,
                _ => false
            };
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;

        return false;
    }
}
