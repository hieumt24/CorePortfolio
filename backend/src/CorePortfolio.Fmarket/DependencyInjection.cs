using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.Fmarket;

public static class DependencyInjection
{
    public static IServiceCollection AddFmarketInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpClient(FmarketConfiguration.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(FmarketConfiguration.GetBaseUrl(configuration));
            client.Timeout = TimeSpan.FromSeconds(FmarketConfiguration.GetTimeoutSeconds(configuration));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://fmarket.vn");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://fmarket.vn/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CorePortfolio/1.0");
        });
        services.AddScoped<IFundNavService, FmarketFundNavService>();
        return services;
    }
}
