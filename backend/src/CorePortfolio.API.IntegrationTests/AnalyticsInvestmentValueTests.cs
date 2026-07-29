using System.Net.Http.Json;
using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class AnalyticsInvestmentValueTests
{
    [Fact]
    public async Task Overview_SeparatesInvestmentHoldingsFromCashInclusiveNav()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("analytics-investment-owner");
        var client = factory.CreateAuthenticatedClient(user.Id);
        var today = DateTime.UtcNow.Date;
        var portfolio = TestData.CreatePortfolio(user, "Investment-only analytics");
        portfolio.CreatedAt = today;
        var category = new AssetCategory
        {
            Id = Guid.NewGuid(),
            Name = "Chứng khoán kiểm thử",
            DefaultCurrency = "VND"
        };
        var marketAsset = new MarketAsset
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Symbol = "TEST",
            Name = "Test investment",
            CurrentPrice = 70_000_000m,
            LastUpdated = DateTime.UtcNow,
            PriceSource = "Manual",
            PriceStatus = "Fresh"
        };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id,
            Portfolio = portfolio,
            MarketAssetId = marketAsset.Id,
            MarketAsset = marketAsset
        };
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id,
            Portfolio = portfolio,
            AssetId = asset.Id,
            Asset = asset,
            Type = TransactionType.Buy,
            Quantity = 1m,
            Price = 60_000_000m,
            Fee = 0m,
            Date = today
        };
        var cashAccount = new CashAccount
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id,
            Portfolio = portfolio,
            Currency = "VND"
        };
        var cashEntry = new CashLedgerEntry
        {
            Id = Guid.NewGuid(),
            CashAccountId = cashAccount.Id,
            CashAccount = cashAccount,
            Amount = 930_000_000m,
            Type = CashLedgerEntryType.OpeningBalance,
            Classification = CashLedgerEntryClassification.OpeningBalance,
            Description = "Regression test cash",
            OccurredAt = today
        };
        var snapshot = new PortfolioSnapshot
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id,
            Portfolio = portfolio,
            Date = today,
            TotalInvested = 60_000_000m,
            TotalValue = 1_000_000_000m,
            HoldingsValue = 70_000_000m,
            CashValue = 930_000_000m,
            NetAssetValue = 1_000_000_000m,
            BaseCurrency = "VND",
            UsdToVndRate = 26_000m,
            ValuationTimestamp = DateTime.UtcNow,
            QualityStatus = "Complete"
        };

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var exchangeRate = await db.SystemSettings
                .SingleOrDefaultAsync(
                    setting => setting.Key == ExchangeRateService.UsdToVndKey,
                    cancellationToken);
            if (exchangeRate is null)
            {
                db.SystemSettings.Add(new SystemSetting
                {
                    Key = ExchangeRateService.UsdToVndKey,
                    Value = "26000",
                    Description = "Integration test exchange rate"
                });
            }
            else
            {
                exchangeRate.Value = "26000";
            }

            db.AddRange(
                user,
                portfolio,
                category,
                marketAsset,
                asset,
                transaction,
                cashAccount,
                cashEntry,
                snapshot);
            await db.SaveChangesAsync(cancellationToken);
        }

        var date = today.ToString("yyyy-MM-dd");
        var overview = await client.GetFromJsonAsync<AnalyticsOverviewDto>(
            $"/api/analytics/overview?portfolioId={portfolio.Id}&from={date}&to={date}&currency=VND",
            cancellationToken);

        Assert.NotNull(overview);
        Assert.Equal(70_000_000m, overview.InvestmentPortfolioValue);
        Assert.Equal(1_000_000_000m, overview.Performance.EndingNetAssetValue);
        Assert.True(overview.Scope.FinancialHealthIsGlobal);
    }
}
