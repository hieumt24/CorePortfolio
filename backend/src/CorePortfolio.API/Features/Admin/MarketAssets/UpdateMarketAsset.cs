using CorePortfolio.Infrastructure.Data;
using MediatR;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record UpdateMarketAssetCommand(Guid Id, Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null) : IRequest<bool>;

public class UpdateMarketAssetHandler : IRequestHandler<UpdateMarketAssetCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public UpdateMarketAssetHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMarketAssetCommand request, CancellationToken cancellationToken)
    {
        var marketAsset = await _dbContext.MarketAssets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (marketAsset == null)
            return false;

        marketAsset.CategoryId = request.CategoryId;
        marketAsset.Symbol = request.Symbol;
        marketAsset.Name = request.Name;
        marketAsset.CurrentPrice = request.CurrentPrice;
        marketAsset.PriceSource = request.PriceSource;
        marketAsset.ExternalId = request.ExternalId;
        if (request.PriceSource.Equals("Manual", StringComparison.OrdinalIgnoreCase))
        {
            marketAsset.PriceStatus = "Manual";
            marketAsset.LastPriceError = null;
            marketAsset.LastUpdated = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
