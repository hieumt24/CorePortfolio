using Microsoft.Extensions.Configuration;

namespace CorePortfolio.KBS;

internal static class KbsConfiguration
{
    internal const string HttpClientName = "KBS";
    private const string DefaultBaseUrl = "https://kbbuddywts.kbsec.com.vn/iis-server/investment/";
    private const int DefaultTimeoutSeconds = 20;
    private const int DefaultLookbackDays = 14;
    private const int DefaultPriceCacheSeconds = 300;
    private const int DefaultInstrumentCacheHours = 6;

    public static string GetBaseUrl(IConfiguration configuration)
    {
        var configured = configuration["KBS:BaseUrl"]?.Trim();
        var value = string.IsNullOrWhiteSpace(configured) ? DefaultBaseUrl : configured;
        return value.EndsWith('/') ? value : $"{value}/";
    }

    public static int GetTimeoutSeconds(IConfiguration configuration) =>
        Math.Clamp(configuration.GetValue("KBS:TimeoutSeconds", DefaultTimeoutSeconds), 5, 60);

    public static int GetLookbackDays(IConfiguration configuration) =>
        Math.Clamp(configuration.GetValue("KBS:LookbackDays", DefaultLookbackDays), 7, 90);

    public static int GetPriceCacheSeconds(IConfiguration configuration) =>
        Math.Clamp(configuration.GetValue("KBS:PriceCacheSeconds", DefaultPriceCacheSeconds), 20, 3600);

    public static int GetInstrumentCacheHours(IConfiguration configuration) =>
        Math.Clamp(configuration.GetValue("KBS:InstrumentCacheHours", DefaultInstrumentCacheHours), 1, 24);
}
