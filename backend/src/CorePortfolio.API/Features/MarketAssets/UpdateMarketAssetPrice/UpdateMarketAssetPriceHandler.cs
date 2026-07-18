using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.API.Services;

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
        var results = new List<PriceRefreshResultDto>();
        foreach (var asset in assets)
        {
            MarketPriceSourceResolver.Normalize(asset);
            decimal? price = null;
            string? error = null;
            try
            {
                if (asset.PriceSource.Equals("DNSE", StringComparison.OrdinalIgnoreCase))
                    price = await _stocks.GetStockPriceAsync(asset.Symbol, cancellationToken);
                else if (asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase))
                    price = string.IsNullOrWhiteSpace(asset.ExternalId) ? null : await _crypto.GetPriceAsync(asset.ExternalId, cancellationToken);
                else error = "Nguồn giá không hỗ trợ tự động cập nhật.";
                if (price is null or <= 0)
                    error ??= string.IsNullOrWhiteSpace(asset.ExternalId) && asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase)
                        ? "Thiếu CoinGecko coin ID." : "Nguồn giá không trả về dữ liệu hợp lệ.";
            }
            catch (Exception exception) { error = exception.Message; }

            if (error == null)
            {
                asset.CurrentPrice = price!.Value;
                asset.LastUpdated = DateTime.UtcNow;
                asset.PriceStatus = "Fresh";
                asset.LastPriceError = null;
            }
            else
            {
                asset.PriceStatus = "Error";
                asset.LastPriceError = error.Length > 500 ? error[..500] : error;
            }
            results.Add(new(asset.Id, asset.Symbol, asset.PriceStatus, price, error));
        }
        await _db.SaveChangesAsync(cancellationToken);
        return results;
    }
}
