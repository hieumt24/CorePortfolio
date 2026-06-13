using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Assets.CreateAsset;

public class CreateAssetHandler : IRequestHandler<CreateAssetCommand, Guid>
{
    private readonly AppDbContext _dbContext;

    public CreateAssetHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId, cancellationToken);
        if (!portfolioExists)
            throw new Exception("Portfolio not found"); // In a real app, use proper exception or Result pattern

        var marketAssetExists = await _dbContext.MarketAssets.AnyAsync(m => m.Id == request.MarketAssetId, cancellationToken);
        if (!marketAssetExists)
            throw new Exception("Market asset not found");

        var assetAlreadyExists = await _dbContext.Assets.AnyAsync(a => a.PortfolioId == request.PortfolioId && a.MarketAssetId == request.MarketAssetId, cancellationToken);
        if (assetAlreadyExists)
            throw new Exception("This asset is already in your portfolio.");

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            PortfolioId = request.PortfolioId,
            MarketAssetId = request.MarketAssetId
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
