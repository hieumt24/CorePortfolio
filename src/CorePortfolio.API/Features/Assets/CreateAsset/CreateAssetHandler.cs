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

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            PortfolioId = request.PortfolioId,
            Symbol = request.Symbol,
            Name = request.Name,
            Type = request.Type,
            CurrentPrice = 0 // Initial price is 0 until updated or first transaction
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
