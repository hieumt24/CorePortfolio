using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Assets.DeleteAsset;

public class DeleteAssetHandler : IRequestHandler<DeleteAssetCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAssetHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // 1. Fetch the asset through its owning portfolio.
        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a =>
                a.Id == request.AssetId &&
                a.PortfolioId == request.PortfolioId &&
                a.Portfolio != null &&
                a.Portfolio.UserId == userId,
                cancellationToken);

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
