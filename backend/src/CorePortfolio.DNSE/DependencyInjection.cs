using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.DNSE;

public static class DependencyInjection
{
    public static IServiceCollection AddDnseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(DnseConfiguration.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(DnseConfiguration.GetBaseUrl(configuration));
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IStockPriceService, DnseStockPriceService>();
        services.AddScoped<IStockInstrumentService, DnseStockInstrumentService>();

        return services;
    }
}
