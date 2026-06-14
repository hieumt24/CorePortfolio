using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record DeleteMarketAssetCommand(Guid Id) : IRequest<bool>;

public class DeleteMarketAssetHandler : IRequestHandler<DeleteMarketAssetCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public DeleteMarketAssetHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMarketAssetCommand request, CancellationToken cancellationToken)
    {
        var marketAsset = await _dbContext.MarketAssets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (marketAsset == null)
            return false;

        try
        {
            _dbContext.MarketAssets.Remove(marketAsset);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Cannot delete this market asset because it is currently included in one or more user portfolios.");
        }
    }
}
