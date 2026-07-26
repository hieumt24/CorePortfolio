using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.API.Services;
using System.Net;

namespace CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;

public class UpdateMarketAssetPriceHandler : IRequestHandler<UpdateMarketAssetPriceCommand>
{
    private readonly AppDbContext _dbContext;

    public UpdateMarketAssetPriceHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateMarketAssetPriceCommand request, CancellationToken cancellationToken)
    {
        var marketAsset = await _dbContext.MarketAssets.FirstOrDefaultAsync(a => a.Id == request.MarketAssetId, cancellationToken);
        
        if (marketAsset == null)
            throw new Exception("Market Asset not found"); // In a real app, use proper exception or Result pattern

        marketAsset.CurrentPrice = request.NewPrice;
        marketAsset.LastUpdated = DateTime.UtcNow;
        marketAsset.PriceSource = "Manual";
        marketAsset.PriceStatus = "Manual";
        marketAsset.LastPriceError = null;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public record RefreshMarketAssetPricesCommand(Guid? MarketAssetId = null) : IRequest<List<PriceRefreshResultDto>>;
public record PriceRefreshResultDto(Guid MarketAssetId, string Symbol, string Status, decimal? Price, string? Error);

public sealed class RefreshMarketAssetPricesHandler : IRequestHandler<RefreshMarketAssetPricesCommand, List<PriceRefreshResultDto>>
{
    private readonly AppDbContext _db;
    private readonly IStockPriceService _stocks;
    private readonly ICryptoPriceService _crypto;
    public RefreshMarketAssetPricesHandler(AppDbContext db, IStockPriceService stocks, ICryptoPriceService crypto)
    { _db = db; _stocks = stocks; _crypto = crypto; }

    public async Task<List<PriceRefreshResultDto>> Handle(RefreshMarketAssetPricesCommand request, CancellationToken cancellationToken)
    {
        var query = _db.MarketAssets.Include(asset => asset.Category).AsQueryable();
        query = request.MarketAssetId.HasValue ? query.Where(m => m.Id == request.MarketAssetId)
            : query.Where(m => m.PriceSource != "Manual");
        var assets = await query.ToListAsync(cancellationToken);
        foreach (var asset in assets)
            MarketPriceSourceResolver.Normalize(asset);
        var cryptoPrices = await _crypto.GetPricesAsync(
            assets
                .Where(asset => asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.ExternalId ?? string.Empty),
            cancellationToken);
        var results = new List<PriceRefreshResultDto>();
        foreach (var asset in assets)
        {
            decimal? price = null;
            string? error = null;
            Exception? providerException = null;
            try
            {
                if (asset.PriceSource.Equals("KBS", StringComparison.OrdinalIgnoreCase))
                    price = await _stocks.GetStockPriceAsync(asset.ExternalId ?? asset.Symbol, cancellationToken);
                else if (asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase))
                    price = !string.IsNullOrWhiteSpace(asset.ExternalId) && cryptoPrices.TryGetValue(asset.ExternalId, out var cryptoPrice)
                        ? cryptoPrice : null;
                else error = "Nguồn giá không hỗ trợ tự động cập nhật.";
                if (price is null or <= 0)
                    error ??= string.IsNullOrWhiteSpace(asset.ExternalId) && asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase)
                        ? "Thiếu CoinGecko coin ID." : "Nguồn giá không trả về dữ liệu hợp lệ.";
            }
            catch (Exception exception)
            {
                providerException = exception;
                error = exception.Message;
            }

            if (error == null)
            {
                asset.CurrentPrice = price!.Value;
                asset.LastUpdated = DateTime.UtcNow;
                asset.PriceStatus = "Fresh";
                asset.LastPriceError = null;
            }
            else
            {
                asset.PriceStatus = providerException is not null && IsTransientProviderError(providerException)
                    ? "Stale"
                    : "Error";
                asset.LastPriceError = error.Length > 500 ? error[..500] : error;
            }
            results.Add(new(asset.Id, asset.Symbol, asset.PriceStatus, price, error));
        }
        await _db.SaveChangesAsync(cancellationToken);
        return results;
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
