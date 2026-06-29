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
            var netCashFlowByCurrency = new Dictionary<string, decimal>();
            var hasFiatAssetByCurrency = new Dictionary<string, bool>();

            // Pass 1: Calculate Net Cash Flows from Non-Fiat assets
            foreach (var asset in portfolio.Assets)
            {
                var categoryName = asset.MarketAsset?.Category?.Name ?? "Unknown";
                var currency = asset.MarketAsset?.Category?.DefaultCurrency ?? "VND";

                if (!netCashFlowByCurrency.ContainsKey(currency))
                    netCashFlowByCurrency[currency] = 0;

                if (categoryName == "Fiat")
                {
                    hasFiatAssetByCurrency[currency] = true;
                    continue;
                }

                var assetTransactions = portfolio.Transactions.Where(t => t.AssetId == asset.Id).ToList();
                foreach (var t in assetTransactions)
                {
                    if (t.Type == TransactionType.Buy)
                        netCashFlowByCurrency[currency] -= t.Quantity * t.Price;
                    else if (t.Type == TransactionType.Sell || t.Type == TransactionType.Dividend)
                        netCashFlowByCurrency[currency] += t.Quantity * t.Price;
                }
            }

            // Pass 2: Calculate Allocations
            foreach (var asset in portfolio.Assets)
            {
                var assetTransactions = portfolio.Transactions.Where(t => t.AssetId == asset.Id).OrderBy(t => t.Date).ToList();
                var marketAsset = asset.MarketAsset;
                var category = marketAsset?.Category;
                var categoryName = category?.Name ?? "Unknown";
                var currency = category?.DefaultCurrency ?? "VND";

                decimal totalQuantity = 0;
                decimal totalCost = 0;
                decimal totalCurrentValue = 0;

                if (categoryName == "Fiat")
                {
                    decimal fiatDeposits = 0;
                    decimal fiatWithdrawals = 0;

                    foreach (var t in assetTransactions)
                    {
                        if (t.Type == TransactionType.Buy || t.Type == TransactionType.Deposit)
                            fiatDeposits += t.Quantity * t.Price;
                        else if (t.Type == TransactionType.Sell || t.Type == TransactionType.Withdrawal)
                            fiatWithdrawals += t.Quantity * t.Price;
                    }

                    totalCost = fiatDeposits - fiatWithdrawals; // Net Fiat Deposited
                    totalQuantity = totalCost + netCashFlowByCurrency[currency]; // Adjust cash balance with stock trades
                    totalCurrentValue = totalQuantity * (marketAsset?.CurrentPrice ?? 1);
                }
                else
                {
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
                        else if (t.Type == TransactionType.Dividend)
                        {
                            totalCost -= t.Quantity * t.Price;
                        }
                    }

                    totalCurrentValue = totalQuantity * (marketAsset?.CurrentPrice ?? 0);
                }

                // Global Category Aggregation by Currency
                var catKey = $"{categoryName}_{currency}";
                if (!categoryAllocationsDict.ContainsKey(catKey))
                {
                    categoryAllocationsDict[catKey] = new CategoryAllocationDto(categoryName, currency, 0, 0);
                }

                var existingCat = categoryAllocationsDict[catKey];
                categoryAllocationsDict[catKey] = existingCat with
                {
                    TotalInvested = existingCat.TotalInvested + totalCost,
                    CurrentValue = existingCat.CurrentValue + totalCurrentValue
                };

                // Portfolio Currency Aggregation
                if (!portfolioCurrenciesDict.ContainsKey(currency))
                {
                    portfolioCurrenciesDict[currency] = new PortfolioCurrencyAllocationDto(currency, 0, 0);
                }

                var existingPortCurr = portfolioCurrenciesDict[currency];
                
                // For Portfolio Currency, if there's no Fiat asset, we fallback to legacy totalCost of non-fiat.
                // If there IS a Fiat asset, the Fiat asset's totalCost (Net Deposits) represents the portfolio's TotalInvested.
                // To avoid double-counting TotalInvested when Fiat exists, we only add totalCost to Portfolio if:
                // a) it's a Fiat asset, OR b) no Fiat asset exists for this currency.
                decimal portfolioAddedCost = 0;
                if (categoryName == "Fiat")
                {
                    portfolioAddedCost = totalCost;
                }
                else if (!hasFiatAssetByCurrency.ContainsKey(currency) || !hasFiatAssetByCurrency[currency])
                {
                    portfolioAddedCost = totalCost;
                }

                portfolioCurrenciesDict[currency] = existingPortCurr with
                {
                    TotalInvested = existingPortCurr.TotalInvested + portfolioAddedCost,
                    CurrentValue = existingPortCurr.CurrentValue + totalCurrentValue
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
