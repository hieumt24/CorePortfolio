using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class TransactionAtomicityTests
{
    [Fact]
    public async Task CreateTransaction_WhenLedgerValidationFails_PersistsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("transaction-owner");
        Portfolio portfolio;
        Asset asset;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (portfolio, asset) = await TestData.SeedTradableAssetAsync(
                db,
                user,
                "Atomic portfolio",
                cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            portfolioId = portfolio.Id,
            assetId = asset.Id,
            type = TransactionType.Buy,
            quantity = 1m,
            price = 1_000_000m,
            currency = "EUR",
            timestamp = DateTime.UtcNow,
            fee = 0m,
            notes = "must roll back"
        }, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verificationDb.Transactions.AnyAsync(cancellationToken));
        Assert.False(await verificationDb.CashLedgerEntries.AnyAsync(cancellationToken));
        Assert.False(await verificationDb.CashAccounts.AnyAsync(cancellationToken));
    }

    [Fact]
    public async Task CreateTransaction_WhenValid_PersistsTransactionAndMatchingLedgerEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("valid-transaction-owner");
        Portfolio portfolio;
        Asset asset;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (portfolio, asset) = await TestData.SeedTradableAssetAsync(
                db,
                user,
                "Consistent portfolio",
                cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            portfolioId = portfolio.Id,
            assetId = asset.Id,
            type = TransactionType.Buy,
            quantity = 2m,
            price = 500_000m,
            currency = "VND",
            timestamp = DateTime.UtcNow,
            fee = 10_000m,
            notes = "consistent write"
        }, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var transaction = await verificationDb.Transactions.SingleAsync(cancellationToken);
        var ledger = await verificationDb.CashLedgerEntries
            .Include(entry => entry.CashAccount)
            .SingleAsync(cancellationToken);
        Assert.Equal(transaction.Id, ledger.TransactionId);
        Assert.Equal(-1_010_000m, ledger.Amount);
        Assert.Equal("VND", ledger.CashAccount.Currency);
    }

    [Fact]
    public async Task CreateTransaction_WithMarketAsset_AddsPortfolioAssetAndPersistsTransactionAtomically()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("auto-asset-transaction-owner");
        var portfolio = TestData.CreatePortfolio(user, "Automatic asset portfolio");
        var marketAssetId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Portfolios.Add(portfolio);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            portfolioId = portfolio.Id,
            marketAssetId,
            type = TransactionType.Buy,
            quantity = 2m,
            price = 500_000m,
            currency = "VND",
            timestamp = DateTime.UtcNow,
            fee = 10_000m,
            notes = "auto attach asset"
        }, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await verificationDb.Assets.SingleAsync(cancellationToken);
        var transaction = await verificationDb.Transactions.SingleAsync(cancellationToken);
        var ledger = await verificationDb.CashLedgerEntries
            .Include(entry => entry.CashAccount)
            .SingleAsync(cancellationToken);
        Assert.Equal(portfolio.Id, asset.PortfolioId);
        Assert.Equal(marketAssetId, asset.MarketAssetId);
        Assert.Equal(asset.Id, transaction.AssetId);
        Assert.Equal(transaction.Id, ledger.TransactionId);
        Assert.Equal(-1_010_000m, ledger.Amount);
        Assert.Equal("VND", ledger.CashAccount.Currency);
    }

    [Fact]
    public async Task CreateTransaction_WithMarketAsset_WhenLedgerValidationFails_RollsBackPortfolioAsset()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("auto-asset-rollback-owner");
        var portfolio = TestData.CreatePortfolio(user, "Automatic asset rollback");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Portfolios.Add(portfolio);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            portfolioId = portfolio.Id,
            marketAssetId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            type = TransactionType.Buy,
            quantity = 1m,
            price = 1_000_000m,
            currency = "EUR",
            timestamp = DateTime.UtcNow,
            fee = 0m
        }, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verificationDb.Assets.AnyAsync(cancellationToken));
        Assert.False(await verificationDb.Transactions.AnyAsync(cancellationToken));
        Assert.False(await verificationDb.CashLedgerEntries.AnyAsync(cancellationToken));
        Assert.False(await verificationDb.CashAccounts.AnyAsync(cancellationToken));
    }
}
