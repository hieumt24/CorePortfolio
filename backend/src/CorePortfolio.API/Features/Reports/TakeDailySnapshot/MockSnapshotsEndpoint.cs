using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Performance;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public static class MockSnapshotsEndpoint
{
    public static void MapMockSnapshotsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports/snapshots/mock", async (
            AppDbContext dbContext,
            ICurrentUserService currentUser,
            ExchangeRateService exchangeRateService,
            CancellationToken cancellationToken) =>
        {
            var userId = currentUser.UserId;
            if (userId is null)
                return Results.Unauthorized();

            var portfolios = await dbContext.Portfolios
                .Where(portfolio => portfolio.UserId == userId)
                .ToListAsync(cancellationToken);
            if (portfolios.Count == 0)
                return Results.BadRequest("No portfolios to mock.");

            var random = new Random(42);
            var today = DateTime.UtcNow.Date;
            var usdToVnd = await exchangeRateService.GetUsdToVndAsync(cancellationToken);

            await using var databaseTransaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var portfolio in portfolios)
            {
                var existing = await dbContext.PortfolioSnapshots
                    .Where(snapshot => snapshot.PortfolioId == portfolio.Id)
                    .ToListAsync(cancellationToken);
                dbContext.PortfolioSnapshots.RemoveRange(existing);

                decimal totalInvested = 100_000_000m;
                decimal netAssetValue = 110_000_000m;

                for (var dayOffset = 30; dayOffset >= 0; dayOffset--)
                {
                    var date = today.AddDays(-dayOffset);
                    totalInvested += (decimal)(random.NextDouble() * 2_000_000 - 500_000);
                    netAssetValue = totalInvested *
                        (decimal)(1 + (random.NextDouble() * 0.4 - 0.15));
                    var cashValue = netAssetValue * 0.1m;
                    var holdingsValue = netAssetValue - cashValue;
                    var totalPnl = netAssetValue - totalInvested;

                    dbContext.PortfolioSnapshots.Add(new PortfolioSnapshot
                    {
                        Id = Guid.NewGuid(),
                        PortfolioId = portfolio.Id,
                        Date = date,
                        TotalInvested = totalInvested,
                        TotalValue = netAssetValue,
                        HoldingsValue = holdingsValue,
                        CashValue = cashValue,
                        NetAssetValue = netAssetValue,
                        NetExternalFlow = dayOffset == 30 ? totalInvested : 0,
                        RealizedPnl = totalPnl * 0.2m,
                        UnrealizedPnl = totalPnl * 0.8m,
                        Income = 0,
                        Fees = 0,
                        BaseCurrency = "VND",
                        UsdToVndRate = usdToVnd,
                        ValuationTimestamp = date.AddHours(23).AddMinutes(55),
                        QualityStatus = PortfolioSnapshotQuality.Complete,
                        StaleAssetCount = 0,
                        UnclassifiedCashFlowCount = 0
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);
            return Results.Ok(new { Message = "Mock data generated successfully." });
        })
        .WithName("MockSnapshots")
        .WithTags("Reports")
        .Produces(StatusCodes.Status200OK);
    }
}
