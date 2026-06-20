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
                decimal totalCost = 0; // Giá trị đầu tư ban đầu (tổng chi phí mua)
                decimal realizedProfitLoss = 0; // Lợi nhuận/lỗ đã thực hiện từ các giao dịch bán
                decimal cashBalance = 0; // Dòng tiền mặt (Tiền nạp/rút, thu từ bán, tốn khi mua, nhận cổ tức)

                foreach (var t in assetTransactions)
                {
                    if (t.Type == TransactionType.Buy)
                    {
                        totalQuantity += t.Quantity;
                        totalCost += t.Quantity * t.Price;
                        cashBalance -= t.Quantity * t.Price; // Giảm dòng tiền mặt khi mua
                    }
                    else if (t.Type == TransactionType.Sell)
                    {
                        if (totalQuantity > 0)
                        {
                            // 1. Tính giá trị vốn trung bình của 1 đơn vị tài sản
                            decimal averageCost = totalCost / totalQuantity;

                            // 2. Trừ đi phần giá vốn của số lượng đem bán
                            totalCost -= averageCost * t.Quantity;

                            // 3. Tính lợi nhuận/lỗ từ giao dịch bán
                            decimal profitLoss = (t.Price - averageCost) * t.Quantity;
                        }

                        totalQuantity -= t.Quantity;
                        cashBalance += t.Quantity * t.Price; // Tăng dòng tiền mặt khi bán

                    }
                    else if (t.Type == TransactionType.Dividend)
                    {
                        totalCost -= t.Quantity * t.Price;
                    }
                }

                // Tính giá trị hiện tại của tài sản
                var assetCurrentPrice = totalQuantity * (marketAsset?.CurrentPrice ?? 0);

                // Tổng tài sản = Giá trị hiện tại của tài sản + Dòng tiền mặt
                var totalCurrentValue = assetCurrentPrice + cashBalance;

                // Global Category Aggregation
                if (!categoryAllocationsDict.ContainsKey(categoryName))
                {
                    categoryAllocationsDict[categoryName] = new CategoryAllocationDto(categoryName, currency, 0, 0);
                }

                var existingCat = categoryAllocationsDict[categoryName];
                categoryAllocationsDict[categoryName] = existingCat with
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
                portfolioCurrenciesDict[currency] = existingPortCurr with
                {
                    TotalInvested = existingPortCurr.TotalInvested + totalCost,
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
