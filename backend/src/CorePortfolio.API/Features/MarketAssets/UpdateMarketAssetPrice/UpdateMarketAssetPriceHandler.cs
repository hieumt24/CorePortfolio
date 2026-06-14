using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
