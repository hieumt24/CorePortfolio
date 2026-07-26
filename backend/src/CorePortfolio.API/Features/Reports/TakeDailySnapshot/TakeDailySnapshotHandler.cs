using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Performance;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public sealed class TakeDailySnapshotHandler(
    AppDbContext dbContext,
    IMediator mediator,
    ExchangeRateService exchangeRateService)
    : IRequestHandler<TakeDailySnapshotCommand, bool>
{
    public async Task<bool> Handle(
        TakeDailySnapshotCommand request,
        CancellationToken cancellationToken)
    {
        var valuationTimestamp = DateTime.UtcNow;
        var snapshotDate = valuationTimestamp.Date;
        var nextDate = snapshotDate.AddDays(1);
        var stalePriceCutoff = valuationTimestamp.AddDays(-2);
        var portfolios = await dbContext.Portfolios
            .AsNoTracking()
            .Select(portfolio => new { portfolio.Id, portfolio.UserId })
            .ToListAsync(cancellationToken);
        var usdToVnd = await exchangeRateService.GetUsdToVndAsync(cancellationToken);

        await using var databaseTransaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var portfolio in portfolios)
        {
            var summary = await mediator.Send(
                new GetPortfolioSummaryQuery(portfolio.Id, portfolio.UserId),
                cancellationToken);
            if (summary is null)
                continue;

            var cashValue = summary.CashBalances.Sum(account =>
                ExchangeRateService.ToVnd(account.Balance, account.Currency, usdToVnd));
            var holdingsValue = summary.CurrentTotalValue;
            var netAssetValue = holdingsValue + cashValue;

            var dailyCashMovements = await dbContext.CashLedgerEntries
                .AsNoTracking()
                .Where(entry =>
                    entry.CashAccount.PortfolioId == portfolio.Id &&
                    entry.OccurredAt >= snapshotDate &&
                    entry.OccurredAt < nextDate)
                .Select(entry => new
                {
                    entry.Amount,
                    entry.Classification,
                    entry.CashAccount.Currency
                })
                .ToListAsync(cancellationToken);

            var netExternalFlow = dailyCashMovements
                .Where(entry => entry.Classification.IsExternalFlow())
                .Sum(entry => ExchangeRateService.ToVnd(
                    entry.Amount,
                    entry.Currency,
                    usdToVnd));
            var unclassifiedCashFlowCount = dailyCashMovements.Count(entry =>
                entry.Classification == CashLedgerEntryClassification.Unknown);

            var dividendTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.PortfolioId == portfolio.Id &&
                    transaction.Type == TransactionType.Dividend &&
                    transaction.Date < nextDate)
                .Select(transaction => new
                {
                    transaction.Quantity,
                    transaction.Price,
                    Currency = transaction.Asset!.MarketAsset!.Category!.DefaultCurrency
                })
                .ToListAsync(cancellationToken);
            var income = dividendTransactions.Sum(transaction =>
                ExchangeRateService.ToVnd(
                    transaction.Quantity * transaction.Price,
                    transaction.Currency,
                    usdToVnd));

            var staleAssetCount = summary.Assets.Count(asset =>
                !AssetCategoryClassifier.IsFiat(asset.CategoryName) &&
                (asset.CurrentPrice <= 0 || asset.PriceUpdatedAt < stalePriceCutoff));
            var qualityStatus = PortfolioSnapshotQuality.Evaluate(
                staleAssetCount,
                unclassifiedCashFlowCount);

            var snapshot = await dbContext.PortfolioSnapshots
                .SingleOrDefaultAsync(item =>
                    item.PortfolioId == portfolio.Id &&
                    item.Date == snapshotDate,
                    cancellationToken);

            if (snapshot is null)
            {
                snapshot = new PortfolioSnapshot
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolio.Id,
                    Date = snapshotDate
                };
                dbContext.PortfolioSnapshots.Add(snapshot);
            }

            ApplySnapshotValues(
                snapshot,
                summary,
                holdingsValue,
                cashValue,
                netAssetValue,
                netExternalFlow,
                income,
                usdToVnd,
                valuationTimestamp,
                qualityStatus,
                staleAssetCount,
                unclassifiedCashFlowCount);
        }

        await CaptureBenchmarkPricesAsync(
            snapshotDate,
            valuationTimestamp,
            stalePriceCutoff,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task CaptureBenchmarkPricesAsync(
        DateTime snapshotDate,
        DateTime valuationTimestamp,
        DateTime stalePriceCutoff,
        CancellationToken cancellationToken)
    {
        var benchmarks = await dbContext.BenchmarkDefinitions
            .Include(benchmark => benchmark.MarketAsset)
            .Where(benchmark =>
                benchmark.IsActive &&
                benchmark.MarketAssetId != null)
            .ToListAsync(cancellationToken);

        foreach (var benchmark in benchmarks)
        {
            var marketAsset = benchmark.MarketAsset;
            if (marketAsset is null || marketAsset.CurrentPrice <= 0)
                continue;

            var pricePoint = await dbContext.BenchmarkPricePoints
                .SingleOrDefaultAsync(item =>
                    item.BenchmarkDefinitionId == benchmark.Id &&
                    item.Date == snapshotDate,
                    cancellationToken);
            if (pricePoint is null)
            {
                pricePoint = new BenchmarkPricePoint
                {
                    Id = Guid.NewGuid(),
                    BenchmarkDefinitionId = benchmark.Id,
                    Date = snapshotDate
                };
                dbContext.BenchmarkPricePoints.Add(pricePoint);
            }

            pricePoint.ClosePrice = marketAsset.CurrentPrice;
            pricePoint.Currency = benchmark.Currency;
            pricePoint.Source = marketAsset.PriceSource;
            pricePoint.QualityStatus =
                marketAsset.LastUpdated < stalePriceCutoff ||
                marketAsset.PriceStatus is "Stale" or "Error"
                    ? PortfolioSnapshotQuality.StalePrices
                    : PortfolioSnapshotQuality.Complete;
            pricePoint.CapturedAt = valuationTimestamp;
        }
    }

    private static void ApplySnapshotValues(
        PortfolioSnapshot snapshot,
        PortfolioSummaryDto summary,
        decimal holdingsValue,
        decimal cashValue,
        decimal netAssetValue,
        decimal netExternalFlow,
        decimal income,
        decimal usdToVnd,
        DateTime valuationTimestamp,
        string qualityStatus,
        int staleAssetCount,
        int unclassifiedCashFlowCount)
    {
        snapshot.TotalInvested = summary.TotalInvested;
        snapshot.TotalValue = netAssetValue;
        snapshot.HoldingsValue = holdingsValue;
        snapshot.CashValue = cashValue;
        snapshot.NetAssetValue = netAssetValue;
        snapshot.NetExternalFlow = netExternalFlow;
        snapshot.RealizedPnl = summary.RealizedPnl;
        snapshot.UnrealizedPnl = summary.UnrealizedPnl;
        snapshot.Income = income;
        snapshot.Fees = summary.Fees;
        snapshot.BaseCurrency = "VND";
        snapshot.UsdToVndRate = usdToVnd;
        snapshot.ValuationTimestamp = valuationTimestamp;
        snapshot.QualityStatus = qualityStatus;
        snapshot.StaleAssetCount = staleAssetCount;
        snapshot.UnclassifiedCashFlowCount = unclassifiedCashFlowCount;
    }
}
