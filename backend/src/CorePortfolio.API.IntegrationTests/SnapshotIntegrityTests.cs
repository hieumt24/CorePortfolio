using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class SnapshotIntegrityTests
{
    [Fact]
    public async Task PortfolioSnapshots_RejectDuplicatePortfolioDate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        _ = factory.CreateClient();
        var user = TestData.CreateUser("snapshot-owner");
        var portfolio = TestData.CreatePortfolio(user, "Snapshot portfolio");
        var snapshotDate = new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(user, portfolio);
        db.PortfolioSnapshots.Add(CreateSnapshot(portfolio.Id, snapshotDate, 1_000_000m));
        await db.SaveChangesAsync(cancellationToken);

        db.PortfolioSnapshots.Add(CreateSnapshot(portfolio.Id, snapshotDate, 1_100_000m));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken));
    }

    private static PortfolioSnapshot CreateSnapshot(Guid portfolioId, DateTime date, decimal totalValue) => new()
    {
        Id = Guid.NewGuid(),
        PortfolioId = portfolioId,
        Date = date,
        TotalInvested = 900_000m,
        TotalValue = totalValue,
        BaseCurrency = "VND",
        UsdToVndRate = 26_000m,
        ValuationTimestamp = DateTime.UtcNow,
        QualityStatus = "Complete"
    };
}
