using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;

namespace CorePortfolio.API.Features.Transactions.GetAssetTransactions;

public class GetAssetTransactionsHandler : IRequestHandler<GetAssetTransactionsQuery, List<TransactionDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAssetTransactionsHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<TransactionDto>> Handle(GetAssetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets
            .AsNoTracking()
            .Where(item => item.Id == request.AssetId &&
                item.Portfolio != null &&
                item.Portfolio.UserId == _currentUserService.UserId)
            .Select(item => new
            {
                CurrentPrice = item.MarketAsset != null ? item.MarketAsset.CurrentPrice : 0,
                CategoryName = item.MarketAsset != null && item.MarketAsset.Category != null
                    ? item.MarketAsset.Category.Name
                    : string.Empty
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (asset == null)
            return [];

        var transactions = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.AssetId == request.AssetId && t.Portfolio != null && t.Portfolio.UserId == _currentUserService.UserId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);

        var acquisitionByTransactionId = PortfolioAccountingCalculator.CalculateBreakdown(
                transactions,
                asset.CurrentPrice,
                AssetCategoryClassifier.IsCrypto(asset.CategoryName))
            .Acquisitions
            .ToDictionary(acquisition => acquisition.TransactionId);

        return transactions.Select(transaction =>
        {
            acquisitionByTransactionId.TryGetValue(transaction.Id, out var acquisition);
            return new TransactionDto(
                transaction.Id,
                (int)transaction.Type,
                transaction.Quantity,
                transaction.Price,
                transaction.Fee,
                transaction.Notes,
                transaction.Date,
                acquisition?.RemainingQuantity,
                acquisition?.UnrealizedPnl,
                acquisition?.IsClosed);
        }).ToList();
    }
}
