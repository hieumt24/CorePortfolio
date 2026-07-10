using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Assets.CreateAsset;

public class CreateAssetHandler : IRequestHandler<CreateAssetCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateAssetHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId && p.UserId == _currentUserService.UserId, cancellationToken);
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
