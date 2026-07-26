using CorePortfolio.API.Features.MarketIndices.GetMarketIndices;
using MediatR;

namespace CorePortfolio.API.Features.MarketIndices;

public static class MarketIndicesEndpoints
{
    private static readonly string[] DefaultSymbols = ["VNINDEX", "VN30"];

    public static void MapMarketIndicesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/market-indices", async (
                string? symbols,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var requested = string.IsNullOrWhiteSpace(symbols)
                    ? DefaultSymbols
                    : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var supported = requested
                    .Where(symbol => DefaultSymbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (supported.Length == 0)
                    return Results.BadRequest(new { message = "Chỉ hỗ trợ VNINDEX và VN30." });

                return Results.Ok(await mediator.Send(
                    new GetMarketIndicesQuery(supported),
                    cancellationToken));
            })
            .RequireAuthorization()
            .WithName("GetMarketIndices")
            .WithTags("Market Indices");
    }
}
