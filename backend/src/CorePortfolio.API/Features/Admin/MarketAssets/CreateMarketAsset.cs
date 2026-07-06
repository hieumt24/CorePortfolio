using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record CreateMarketAssetCommand(Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null) : IRequest<Guid>;

public class CreateMarketAssetHandler : IRequestHandler<CreateMarketAssetCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    public CreateMarketAssetHandler(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Guid> Handle(CreateMarketAssetCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _dbContext.AssetCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists) throw new Exception("Category not found");

        var marketAsset = new MarketAsset
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Symbol = request.Symbol,
            Name = request.Name,
            CurrentPrice = request.CurrentPrice,
            LastUpdated = DateTime.UtcNow,
            PriceSource = request.PriceSource,
            ExternalId = request.ExternalId,
            PriceStatus = "Manual"
        };
        _dbContext.MarketAssets.Add(marketAsset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return marketAsset.Id;
    }
}
