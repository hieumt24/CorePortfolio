using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;

public class GetPortfolioSummaryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto?>
{
    private readonly AppDbContext _dbContext;

    public GetPortfolioSummaryHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PortfolioSummaryDto?> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        var portfolio = await _dbContext.Portfolios
            .Include(p => p.Assets)
            .Include(p => p.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PortfolioId, cancellationToken);

        if (portfolio == null)
            return null;

        var assetSummaries = new List<AssetSummaryDto>();
        decimal totalInvested = 0;
        decimal currentTotalValue = 0;

        foreach (var asset in portfolio.Assets)
        {
            var assetTransactions = portfolio.Transactions.Where(t => t.AssetId == asset.Id).ToList();
            
            decimal totalQuantity = 0;
            decimal totalCost = 0;

            foreach (var t in assetTransactions)
            {
                if (t.Type == TransactionType.Buy)
                {
                    totalQuantity += t.Quantity;
                    totalCost += t.Quantity * t.Price;
                }
                else if (t.Type == TransactionType.Sell)
                {
                    totalQuantity -= t.Quantity;
                    // For simplicity, we just reduce cost proportionally or just calculate cost as total buys minus total sells.
                    // This is a naive calculation. A real app might use FIFO.
                    totalCost -= t.Quantity * t.Price; 
                }
            }

            var currentValue = totalQuantity * asset.CurrentPrice;
            totalInvested += totalCost;
            currentTotalValue += currentValue;

            assetSummaries.Add(new AssetSummaryDto(
                asset.Id,
                asset.Symbol,
                asset.Name,
                asset.Type,
                asset.Currency,
                asset.CurrentPrice,
                totalQuantity,
                totalCost,
                currentValue
            ));
        }

        return new PortfolioSummaryDto(portfolio.Id, portfolio.Name, totalInvested, currentTotalValue, assetSummaries);
    }
}
