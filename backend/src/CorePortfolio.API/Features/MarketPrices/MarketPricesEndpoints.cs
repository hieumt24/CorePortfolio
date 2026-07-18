using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.MarketPrices;

public static class MarketPricesEndpoints
{
    public static void MapMarketPricesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/market-prices/status", async (AppDbContext db, ICurrentUserService current, CancellationToken cancellationToken) =>
        {
            var userId = current.UserId ?? throw new UnauthorizedAccessException();
            var assets = await db.Assets.AsNoTracking()
                .Where(asset => asset.Portfolio!.UserId == userId)
                .Select(asset => asset.MarketAsset)
                .Where(asset => asset != null)
                .Distinct()
                .Select(asset => new MarketPriceStatusDto(
                    asset!.Id, asset!.Symbol, asset!.PriceSource, asset!.CurrentPrice,
                    asset!.LastUpdated, asset!.PriceStatus, asset!.LastPriceError))
                .OrderBy(asset => asset.Symbol)
                .ToListAsync(cancellationToken);
            return Results.Ok(assets);
        }).RequireAuthorization();
    }
}

public record MarketPriceStatusDto(Guid Id, string Symbol, string PriceSource, decimal CurrentPrice,
    DateTime LastUpdated, string PriceStatus, string? LastPriceError);
