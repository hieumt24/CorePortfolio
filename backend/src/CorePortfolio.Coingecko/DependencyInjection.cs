using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.Coingecko;

public static class DependencyInjection
{
    public static IServiceCollection AddCoinGeckoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpClient("CoinGecko");
        services.AddScoped<ICryptoPriceService, CoinGeckoService>();
        services.AddScoped<ICryptoMarketService>(provider =>
            (CoinGeckoService)provider.GetRequiredService<ICryptoPriceService>());
        
        return services;
    }
}
