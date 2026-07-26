using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.KBS;

public static class DependencyInjection
{
    public static IServiceCollection AddKbsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpClient(KbsConfiguration.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(KbsConfiguration.GetBaseUrl(configuration));
            client.Timeout = TimeSpan.FromSeconds(KbsConfiguration.GetTimeoutSeconds(configuration));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
        });

        services.AddScoped<KbsStockPriceService>();
        services.AddScoped<IStockPriceService>(provider => provider.GetRequiredService<KbsStockPriceService>());
        services.AddScoped<IPriceProvider>(provider => provider.GetRequiredService<KbsStockPriceService>());
        services.AddScoped<IStockInstrumentService, KbsStockInstrumentService>();
        return services;
    }
}
