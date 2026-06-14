using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Assets.DeleteAsset;

public class DeleteAssetHandler : IRequestHandler<DeleteAssetCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public DeleteAssetHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the asset
        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.PortfolioId == request.PortfolioId, cancellationToken);

        if (asset == null)
            return false;

        // 2. Fetch all transactions for this asset
        var transactions = await _dbContext.Transactions
            .Where(t => t.AssetId == request.AssetId && t.PortfolioId == request.PortfolioId)
            .ToListAsync(cancellationToken);

        // 3. Delete transactions to satisfy Restrict constraint
        if (transactions.Any())
        {
            _dbContext.Transactions.RemoveRange(transactions);
        }

        // 4. Delete the asset
        _dbContext.Assets.Remove(asset);

        // 5. Save changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
