using Microsoft.Extensions.Configuration;

namespace CorePortfolio.Fmarket;

internal static class FmarketConfiguration
{
    internal const string HttpClientName = "Fmarket";

    public static string GetBaseUrl(IConfiguration configuration)
    {
        var value = configuration["Fmarket:BaseUrl"]?.Trim();
        value = string.IsNullOrWhiteSpace(value) ? "https://api.fmarket.vn/" : value;
        return value.EndsWith('/') ? value : $"{value}/";
    }

    public static int GetTimeoutSeconds(IConfiguration configuration) =>
        Math.Clamp(configuration.GetValue("Fmarket:TimeoutSeconds", 20), 5, 60);

    public static int GetCacheMinutes(IConfiguration configuration) =>
        Math.Clamp(configuration.GetValue("Fmarket:FundCacheMinutes", 60), 5, 360);
}
