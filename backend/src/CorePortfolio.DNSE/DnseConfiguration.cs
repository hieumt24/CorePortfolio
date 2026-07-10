using Microsoft.Extensions.Configuration;

namespace CorePortfolio.DNSE;

internal static class DnseConfiguration
{
    internal const string HttpClientName = "DNSE";
    private const string DefaultBaseUrl = "https://openapi.dnse.com.vn";
    private const string DefaultApiVersion = "2026-05-07";

    public static string GetBaseUrl(IConfiguration configuration)
    {
        return GetFirstConfiguredValue(configuration,
            "DNSE:BaseUrl",
            "Dnse:BaseUrl",
            "DNSE__BaseUrl",
            "DNSE_BASE_URL") ?? DefaultBaseUrl;
    }

    public static string GetApiVersion(IConfiguration configuration)
    {
        return GetFirstConfiguredValue(configuration,
            "DNSE:ApiVersion",
            "Dnse:ApiVersion",
            "DNSE__ApiVersion",
            "DNSE_API_VERSION") ?? DefaultApiVersion;
    }

    public static bool TryGetCredentials(
        IConfiguration configuration,
        out string apiKey,
        out string secretKey,
        out string missingSetting)
    {
        apiKey = GetFirstConfiguredValue(configuration,
            "DNSE:ApiKey",
            "Dnse:ApiKey",
            "DNSE__ApiKey",
            "DNSE_API_KEY",
            "DNSE_APIKEY") ?? string.Empty;

        secretKey = GetFirstConfiguredValue(configuration,
            "DNSE:SecretKey",
            "Dnse:SecretKey",
            "DNSE__SecretKey",
            "DNSE_SECRET_KEY",
            "DNSE_SECRETKEY") ?? string.Empty;

        missingSetting = (string.IsNullOrWhiteSpace(apiKey), string.IsNullOrWhiteSpace(secretKey)) switch
        {
            (true, true) => "ApiKey, SecretKey",
            (true, false) => "ApiKey",
            (false, true) => "SecretKey",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(missingSetting);
    }

    private static string? GetFirstConfiguredValue(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var configuredValue = configuration[key];
            if (!string.IsNullOrWhiteSpace(configuredValue))
                return configuredValue.Trim();

            var environmentValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(environmentValue))
                return environmentValue.Trim();
        }

        return null;
    }
}
