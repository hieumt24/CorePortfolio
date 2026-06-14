using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Reports.GetGlobalReport;

public class GetGlobalReportHandler : IRequestHandler<GetGlobalReportQuery, GlobalReportDto>
{
    private readonly AppDbContext _dbContext;
    public GetGlobalReportHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GlobalReportDto> Handle(GetGlobalReportQuery request, CancellationToken cancellationToken)
    {
        var portfolios = await _dbContext.Portfolios
            .Include(p => p.Assets)
                .ThenInclude(a => a.MarketAsset)
                    .ThenInclude(ma => ma.Category)
            .Include(p => p.Transactions)
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        var categoryAllocationsDict = new Dictionary<string, CategoryAllocationDto>();
        var portfolioAllocations = new List<PortfolioAllocationDto>();

        foreach (var portfolio in portfolios)
        {
            var portfolioCurrenciesDict = new Dictionary<string, PortfolioCurrencyAllocationDto>();

            foreach (var asset in portfolio.Assets)
            {
                var assetTransactions = portfolio.Transactions.Where(t => t.AssetId == asset.Id).ToList();
                var marketAsset = asset.MarketAsset;
                var category = marketAsset?.Category;
                var categoryName = category?.Name ?? "Unknown";
                var currency = category?.DefaultCurrency ?? "VND";

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
                        totalCost -= t.Quantity * t.Price;
                    }
                }

                var currentValue = totalQuantity * (marketAsset?.CurrentPrice ?? 0);

                // Global Category Aggregation
                if (!categoryAllocationsDict.ContainsKey(categoryName))
                {
                    categoryAllocationsDict[categoryName] = new CategoryAllocationDto(categoryName, currency, 0, 0);
                }

                var existingCat = categoryAllocationsDict[categoryName];
                categoryAllocationsDict[categoryName] = existingCat with
                {
                    TotalInvested = existingCat.TotalInvested + totalCost,
                    CurrentValue = existingCat.CurrentValue + currentValue
                };

                // Portfolio Currency Aggregation
                if (!portfolioCurrenciesDict.ContainsKey(currency))
                {
                    portfolioCurrenciesDict[currency] = new PortfolioCurrencyAllocationDto(currency, 0, 0);
                }

                var existingPortCurr = portfolioCurrenciesDict[currency];
                portfolioCurrenciesDict[currency] = existingPortCurr with
                {
                    TotalInvested = existingPortCurr.TotalInvested + totalCost,
                    CurrentValue = existingPortCurr.CurrentValue + currentValue
                };
            }

            portfolioAllocations.Add(new PortfolioAllocationDto(
                portfolio.Id,
                portfolio.Name,
                portfolioCurrenciesDict.Values.ToList()
            ));
        }

        return new GlobalReportDto(
            categoryAllocationsDict.Values.ToList(),
            portfolioAllocations
        );
    }
}
