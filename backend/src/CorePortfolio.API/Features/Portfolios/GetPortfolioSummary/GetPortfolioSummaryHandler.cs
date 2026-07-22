using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;

public class GetPortfolioSummaryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto?>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ExchangeRateService _exchangeRateService;

    public GetPortfolioSummaryHandler(AppDbContext dbContext, ICurrentUserService currentUserService, ExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<PortfolioSummaryDto?> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? _currentUserService.UserId;
        if (userId == null) throw new UnauthorizedAccessException();
        var portfolio = await _dbContext.Portfolios
            .Include(p => p.Assets).ThenInclude(a => a.MarketAsset).ThenInclude(m => m!.Category)
            .Include(p => p.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PortfolioId && p.UserId == userId, cancellationToken);
        if (portfolio == null) return null;

        var accounts = await _dbContext.CashAccounts.AsNoTracking()
            .Where(a => a.PortfolioId == portfolio.Id)
            .Select(a => new CashBalanceDto(a.Id, a.Currency, a.Entries.Sum(e => e.Amount)))
            .ToListAsync(cancellationToken);

        var usdToVnd = await _exchangeRateService.GetUsdToVndAsync(cancellationToken);
        var assets = new List<AssetSummaryDto>();
        decimal totalInvested = 0, totalValue = 0, realized = 0, unrealized = 0, fees = 0;
        foreach (var asset in portfolio.Assets)
        {
            var marketAsset = asset.MarketAsset;
            var category = marketAsset?.Category;
            var result = PortfolioAccountingCalculator.Calculate(
                portfolio.Transactions.Where(t => t.AssetId == asset.Id), marketAsset?.CurrentPrice ?? 0,
                AssetCategoryClassifier.IsCrypto(category?.Name));

            var currency = category?.DefaultCurrency ?? "VND";
            totalInvested += ExchangeRateService.ToVnd(result.CostBasis, currency, usdToVnd);
            totalValue += ExchangeRateService.ToVnd(result.CurrentValue, currency, usdToVnd);
            realized += ExchangeRateService.ToVnd(result.RealizedPnl, currency, usdToVnd);
            unrealized += ExchangeRateService.ToVnd(result.UnrealizedPnl, currency, usdToVnd);
            fees += ExchangeRateService.ToVnd(result.Fees, currency, usdToVnd);
            assets.Add(new AssetSummaryDto(asset.Id, asset.MarketAssetId, marketAsset?.Symbol ?? "N/A",
                marketAsset?.Name ?? "N/A", category?.Name ?? "Unknown", currency,
                marketAsset?.CurrentPrice ?? 0, result.Quantity, result.CostBasis, result.CurrentValue,
                result.TotalBought, result.AverageCost, result.RealizedPnl, result.UnrealizedPnl, result.Fees,
                marketAsset?.LastUpdated ?? DateTime.MinValue));
        }

        totalValue += accounts.Sum(c => ExchangeRateService.ToVnd(c.Balance, c.Currency, usdToVnd));

        return new PortfolioSummaryDto(portfolio.Id, portfolio.Name, totalInvested, totalValue, assets,
            accounts, realized, unrealized, fees, "VND", DateTime.UtcNow);
    }
}
