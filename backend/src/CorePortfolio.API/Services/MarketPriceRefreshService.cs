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
    private readonly ProductionOperationsState _operationsState;

    public MarketPriceRefreshService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MarketPriceRefreshService> logger,
        ProductionOperationsState operationsState)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _operationsState = operationsState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("MarketPrices:Enabled", true)) return;
        var cryptoInterval = Math.Clamp(_configuration.GetValue("MarketPrices:CryptoRefreshIntervalSeconds", 1800), 300, 86400);
        var stockInterval = Math.Clamp(_configuration.GetValue("MarketPrices:StockRefreshIntervalSeconds", 1800), 300, 86400);
        var fundInterval = Math.Clamp(_configuration.GetValue("MarketPrices:FundRefreshIntervalSeconds", 21600), 900, 86400);
        var timerInterval = Math.Min(Math.Min(cryptoInterval, stockInterval), fundInterval);
        await RefreshAsync(cryptoInterval, stockInterval, fundInterval, stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(timerInterval));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RefreshAsync(cryptoInterval, stockInterval, fundInterval, stoppingToken);
    }

    private async Task RefreshAsync(
        int cryptoIntervalSeconds,
        int stockIntervalSeconds,
        int fundIntervalSeconds,
        CancellationToken cancellationToken)
    {
        var startedAt = _operationsState.StartJob("MarketPriceRefresh");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var crypto = scope.ServiceProvider.GetRequiredService<ICryptoPriceService>();
            var stocks = scope.ServiceProvider.GetRequiredService<IStockPriceService>();
            var funds = scope.ServiceProvider.GetRequiredService<IFundNavService>();
            var assets = await db.MarketAssets
                .Include(asset => asset.Category)
                .Where(asset =>
                    asset.PriceSource == "CoinGecko"
                    || asset.PriceSource == "KBS"
                    || asset.PriceSource == "DNSE"
                    || asset.PriceSource == "Fmarket"
                    || asset.PriceSource == "")
                .ToListAsync(cancellationToken);

            foreach (var asset in assets)
                MarketPriceSourceResolver.Normalize(asset);

            var cryptoRefreshBefore = DateTime.UtcNow.AddSeconds(-cryptoIntervalSeconds);
            var refreshableCryptoAssets = assets
                .Where(asset => asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(asset.ExternalId)
                    && asset.LastUpdated <= cryptoRefreshBefore)
                .ToList();
            var prices = await crypto.GetPricesAsync(
                refreshableCryptoAssets.Select(asset => asset.ExternalId!),
                cancellationToken);

            foreach (var asset in refreshableCryptoAssets)
            {
                try
                {
                    if (prices.TryGetValue(asset.ExternalId!, out var price) && price > 0)
                    {
                        asset.CurrentPrice = price;
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

            var stockRefreshBefore = DateTime.UtcNow.AddSeconds(-stockIntervalSeconds);
            var refreshableStockAssets = assets
                .Where(asset => asset.PriceSource.Equals("KBS", StringComparison.OrdinalIgnoreCase)
                    && asset.LastUpdated <= stockRefreshBefore)
                .ToList();

            foreach (var asset in refreshableStockAssets)
            {
                try
                {
                    var price = await stocks.GetStockPriceAsync(asset.ExternalId ?? asset.Symbol, cancellationToken);
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
                        asset.LastPriceError = "KBS không trả về giá hợp lệ.";
                    }
                }
                catch (Exception exception)
                {
                    asset.PriceStatus = IsTransientProviderError(exception) ? "Stale" : "Error";
                    asset.LastPriceError = exception.Message[..Math.Min(exception.Message.Length, 500)];
                    _logger.LogWarning(exception, "Failed to refresh KBS stock price for {AssetId}.", asset.Id);
                }
            }

            var fundRefreshBefore = DateTime.UtcNow.AddSeconds(-fundIntervalSeconds);
            var refreshableFundAssets = assets
                .Where(asset => asset.PriceSource.Equals("Fmarket", StringComparison.OrdinalIgnoreCase)
                    && asset.LastUpdated <= fundRefreshBefore)
                .ToList();
            if (refreshableFundAssets.Count > 0)
            {
                try
                {
                    var fundUniverse = await funds.GetFundsAsync(cancellationToken);
                    var byExternalId = fundUniverse.ToDictionary(
                        item => item.ExternalId, StringComparer.OrdinalIgnoreCase);
                    var bySymbol = fundUniverse.ToDictionary(
                        item => item.Symbol, StringComparer.OrdinalIgnoreCase);
                    foreach (var asset in refreshableFundAssets)
                    {
                        FundNavInstrument? fund = null;
                        var found = !string.IsNullOrWhiteSpace(asset.ExternalId)
                            && byExternalId.TryGetValue(asset.ExternalId, out fund);
                        if (!found)
                            found = bySymbol.TryGetValue(asset.Symbol, out fund);
                        if (found && fund!.Nav > 0)
                        {
                            asset.CurrentPrice = fund.Nav;
                            asset.LastUpdated = fund.AsOf;
                            asset.PriceStatus = "Fresh";
                            asset.LastPriceError = null;
                        }
                        else
                        {
                            asset.PriceStatus = "Stale";
                            asset.LastPriceError = "Fmarket không trả về NAV hợp lệ.";
                        }
                    }
                }
                catch (Exception exception)
                {
                    foreach (var asset in refreshableFundAssets)
                    {
                        asset.PriceStatus = IsTransientProviderError(exception) ? "Stale" : "Error";
                        asset.LastPriceError = exception.Message[..Math.Min(exception.Message.Length, 500)];
                    }
                    _logger.LogWarning(exception, "Failed to refresh Fmarket fund NAV.");
                }
            }

            if (assets.Count > 0)
                await db.SaveChangesAsync(cancellationToken);
            _operationsState.CompleteJob("MarketPriceRefresh", startedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _operationsState.FailJob("MarketPriceRefresh", startedAt, exception);
            _logger.LogError(exception, "Market price refresh cycle failed.");
        }
    }

    private static bool IsTransientProviderError(Exception exception)
    {
        if (exception is TimeoutException or OperationCanceledException)
            return true;
        if (exception is not HttpRequestException httpException)
            return false;

        return httpException.StatusCode is null
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}
