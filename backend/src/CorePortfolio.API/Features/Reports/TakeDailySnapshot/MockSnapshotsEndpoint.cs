using CorePortfolio.Infrastructure.Data;
using CorePortfolio.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public static class MockSnapshotsEndpoint
{
    public static void MapMockSnapshotsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports/snapshots/mock", async (AppDbContext dbContext) =>
        {
            var portfolios = await dbContext.Portfolios.ToListAsync();
            if (!portfolios.Any()) return Results.BadRequest("No portfolios to mock.");

            var random = new Random();
            var today = DateTime.UtcNow.Date;

            foreach (var p in portfolios)
            {
                // Delete existing snapshots
                var existing = await dbContext.PortfolioSnapshots.Where(s => s.PortfolioId == p.Id).ToListAsync();
                dbContext.PortfolioSnapshots.RemoveRange(existing);

                decimal baseInvested = 100000000; // 100M VND
                decimal baseValue = 110000000;

                for (int i = 30; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    // Add some random walk
                    baseInvested += (decimal)(random.NextDouble() * 2000000 - 500000); // mostly going up
                    baseValue = baseInvested * (decimal)(1 + (random.NextDouble() * 0.4 - 0.15)); // +/- 15-40% 

                    var snapshot = new PortfolioSnapshot
                    {
                        Id = Guid.NewGuid(),
                        PortfolioId = p.Id,
                        Date = date,
                        TotalInvested = baseInvested,
                        TotalValue = baseValue
                    };
                    dbContext.PortfolioSnapshots.Add(snapshot);
                }
            }

            await dbContext.SaveChangesAsync();
            return Results.Ok(new { Message = "Mock data generated successfully." });
        })
        .WithName("MockSnapshots")
        .WithTags("Reports")
        .Produces(StatusCodes.Status200OK);
    }
}
