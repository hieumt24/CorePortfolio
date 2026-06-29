using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;

public class GetPortfolioSummaryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto?>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetPortfolioSummaryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioSummaryDto?> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        var portfolio = await _dbContext.Portfolios
            .Include(p => p.Assets)
                .ThenInclude(a => a.MarketAsset)
                    .ThenInclude(ma => ma.Category)
            .Include(p => p.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PortfolioId && p.UserId == _currentUserService.UserId, cancellationToken);

        if (portfolio == null)
            return null;

        var assetSummaries = new List<AssetSummaryDto>();
        decimal totalInvested = 0;
        decimal currentTotalValue = 0;

        var netCashFlowByCurrency = new Dictionary<string, decimal>();
        var hasFiatAssetByCurrency = new Dictionary<string, bool>();

        // Pass 1: Calculate Net Cash Flows from Non-Fiat assets
        foreach (var asset in portfolio.Assets)
        {
            var marketAsset = asset.MarketAsset;
            var categoryName = marketAsset?.Category?.Name ?? "Unknown";
            var currency = marketAsset?.Category?.DefaultCurrency ?? "VND";

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

        // Pass 2: Build Asset Summaries
        foreach (var asset in portfolio.Assets)
        {
            var assetTransactions = portfolio.Transactions.Where(t => t.AssetId == asset.Id).OrderBy(t => t.Date).ToList();
            var marketAsset = asset.MarketAsset;
            var categoryName = marketAsset?.Category?.Name ?? "Unknown";
            var currency = marketAsset?.Category?.DefaultCurrency ?? "VND";
            
            decimal totalQuantity = 0;
            decimal totalCost = 0;
            decimal totalBought = 0;

            if (categoryName == "Fiat")
            {
                // Fiat Asset logic (Cash)
                decimal fiatDeposits = 0;
                decimal fiatWithdrawals = 0;

                foreach (var t in assetTransactions)
                {
                    if (t.Type == TransactionType.Buy || t.Type == TransactionType.Deposit)
                    {
                        fiatDeposits += t.Quantity * t.Price;
                        totalBought += t.Quantity * t.Price;
                    }
                    else if (t.Type == TransactionType.Sell || t.Type == TransactionType.Withdrawal)
                    {
                        fiatWithdrawals += t.Quantity * t.Price;
                    }
                }

                totalCost = fiatDeposits - fiatWithdrawals; // Net Fiat Deposited
                totalQuantity = totalCost + netCashFlowByCurrency[currency]; // Adjust cash balance with stock trades
                
                // If the user hasn't explicitly recorded enough fiat deposits to cover their stock purchases,
                // their cash balance will go negative. We implicitly treat this deficit as virtual deposits
                // to maintain correct Total Invested and Total Value figures.
                if (totalQuantity < 0)
                {
                    decimal virtualDeposits = -totalQuantity;
                    totalQuantity = 0; // Clamp cash balance to 0
                    totalCost += virtualDeposits; // Add to Total Invested
                }

                var currentValue = totalQuantity * (marketAsset?.CurrentPrice ?? 1);
                totalInvested += totalCost;
                currentTotalValue += currentValue;

                assetSummaries.Add(new AssetSummaryDto(
                    asset.Id, asset.MarketAssetId, marketAsset?.Symbol ?? "N/A", marketAsset?.Name ?? "N/A",
                    categoryName, currency, marketAsset?.CurrentPrice ?? 1, totalQuantity, totalCost, currentValue, totalBought
                ));
            }
            else
            {
                // Non-Fiat Asset logic (Stocks, Crypto, etc.) using Net Cash Flow (Total PNL method)
                foreach (var t in assetTransactions)
                {
                    if (t.Type == TransactionType.Buy)
                    {
                        totalQuantity += t.Quantity;
                        totalCost += t.Quantity * t.Price;
                        totalBought += t.Quantity * t.Price;
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

                var currentValue = totalQuantity * (marketAsset?.CurrentPrice ?? 0);
                
                // If there is NO Fiat asset for this currency, we add this asset's Net Cash Flow (totalCost) to Portfolio's totalInvested 
                // to maintain backwards compatibility for users who haven't added Fiat assets yet.
                if (!hasFiatAssetByCurrency.ContainsKey(currency) || !hasFiatAssetByCurrency[currency])
                {
                    totalInvested += totalCost;
                }

                currentTotalValue += currentValue;

                assetSummaries.Add(new AssetSummaryDto(
                    asset.Id, asset.MarketAssetId, marketAsset?.Symbol ?? "N/A", marketAsset?.Name ?? "N/A",
                    categoryName, currency, marketAsset?.CurrentPrice ?? 0, totalQuantity, totalCost, currentValue, totalBought
                ));
            }
        }



        return new PortfolioSummaryDto(portfolio.Id, portfolio.Name, totalInvested, currentTotalValue, assetSummaries);
    }
}
