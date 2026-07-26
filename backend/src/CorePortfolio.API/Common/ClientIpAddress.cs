using System.Net;

namespace CorePortfolio.API.Common;

public static class ClientIpAddress
{
    public static string? Resolve(HttpContext? httpContext)
    {
        var address = httpContext?.Connection.RemoteIpAddress;
        if (address is null)
            return null;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var value = address.ToString();
        return value.Length <= 45 ? value : null;
    }
}
