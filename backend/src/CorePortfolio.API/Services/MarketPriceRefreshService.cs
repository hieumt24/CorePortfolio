using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CorePortfolio.API.Services;

public sealed class MarketPriceRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MarketPriceRefreshService> _logger;

    public MarketPriceRefreshService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<MarketPriceRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("MarketPrices:Enabled", true)) return;
        var interval = Math.Clamp(_configuration.GetValue("MarketPrices:CryptoRefreshIntervalSeconds", 60), 30, 3600);
        await RefreshAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RefreshAsync(stoppingToken);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var crypto = scope.ServiceProvider.GetRequiredService<ICryptoPriceService>();
            var assets = await db.MarketAssets
                .Include(asset => asset.Category)
                .Where(asset => asset.PriceSource == "CoinGecko" || asset.PriceSource == "")
                .ToListAsync(cancellationToken);

            foreach (var asset in assets)
            {
                MarketPriceSourceResolver.Normalize(asset);
                if (!asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(asset.ExternalId))
                    continue;
                try
                {
                    var price = await crypto.GetPriceAsync(asset.ExternalId!, cancellationToken);
                    if (price is > 0)
                    {
                        asset.CurrentPrice = price.Value;
                        asset.LastUpdated = DateTime.UtcNow;
                        asset.PriceStatus = "Fresh";
                        asset.LastPriceError = null;
                    }
                    else
                    {
                        asset.PriceStatus = "Stale";
                        asset.LastPriceError = "CoinGecko không trả về giá hợp lệ.";
                    }
                }
                catch (Exception exception)
                {
                    asset.PriceStatus = IsTransientProviderError(exception) ? "Stale" : "Error";
                    asset.LastPriceError = exception.Message[..Math.Min(exception.Message.Length, 500)];
                    _logger.LogWarning(exception, "Failed to refresh crypto price for {AssetId}.", asset.Id);
                }
            }

            if (assets.Count > 0)
                await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Market price refresh cycle failed.");
        }
    }

    private static bool IsTransientProviderError(Exception exception) =>
        exception is TimeoutException
        || exception is OperationCanceledException
        || exception is HttpRequestException
        {
            StatusCode: HttpStatusCode.TooManyRequests
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout
        };
}
