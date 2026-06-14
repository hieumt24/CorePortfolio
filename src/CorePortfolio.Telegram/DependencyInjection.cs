using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CorePortfolio.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<TelegramBotService>();
        return services;
    }
}
