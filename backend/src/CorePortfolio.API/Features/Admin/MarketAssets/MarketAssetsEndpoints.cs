using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;
using System.Net;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record CreateMarketAssetRequest(Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null);

public record UpdateMarketAssetRequest(Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null);

public static class MarketAssetsEndpoints
{
    public static void MapMarketAssetsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/market-assets")
            .WithTags("Admin Market Assets");

        group.MapPost("/", async ([FromBody] CreateMarketAssetRequest request, IMediator mediator) =>
        {
            var id = await mediator.Send(new CreateMarketAssetCommand(request.CategoryId, request.Symbol, request.Name,
                request.CurrentPrice, request.PriceSource, request.ExternalId));
            return Results.Created($"/api/admin/market-assets/{id}", new { Id = id });
        }).RequireAuthorization("Admin");

        group.MapPut("/{id}", async (Guid id, [FromBody] UpdateMarketAssetRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new UpdateMarketAssetCommand(id, request.CategoryId, request.Symbol, request.Name,
                request.CurrentPrice, request.PriceSource, request.ExternalId));
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("Admin");

        group.MapDelete("/{id}", async (Guid id, IMediator mediator) =>
        {
            try
            {
                var success = await mediator.Send(new DeleteMarketAssetCommand(id));
                return success ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization("Admin");

        group.MapGet("/", async (IMediator mediator, [FromQuery] Guid? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
        {
            var result = await mediator.Send(new GetMarketAssetsQuery(categoryId, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/coingecko-price/{coinId}", async (string coinId, IMediator mediator) =>
        {
            var price = await mediator.Send(new GetCoinGeckoPriceQuery(coinId));
            return price.HasValue ? Results.Ok(new { Price = price.Value }) : Results.NotFound();
        });

        group.MapGet("/dnse-price/{symbol}", async (string symbol, IMediator mediator) =>
        {
            try
            {
                var price = await mediator.Send(new GetDnseStockPriceQuery(symbol));
                return price.HasValue
                    ? Results.Ok(new { Price = price.Value })
                    : Results.NotFound(new { message = $"DNSE không trả về giá hợp lệ cho mã {symbol.ToUpperInvariant()}." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "DNSE chưa được cấu hình", detail: ex.Message);
            }
            catch (HttpRequestException ex)
            {
                var statusCode = ex.StatusCode == HttpStatusCode.NotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status502BadGateway;
                return Results.Problem(statusCode: statusCode, title: "Không lấy được giá từ DNSE", detail: ex.Message);
            }
        });

        group.MapGet("/dnse-instruments", async ([FromQuery] string? query, IMediator mediator) =>
        {
            var result = await mediator.Send(new SearchDnseInstrumentsQuery { Query = query ?? string.Empty });
            return Results.Ok(result);
        });

        group.MapPost("/refresh", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new RefreshMarketAssetPricesCommand())))
            .RequireAuthorization("Admin");

        group.MapPost("/{id:guid}/refresh", async (Guid id, IMediator mediator) =>
            Results.Ok(await mediator.Send(new RefreshMarketAssetPricesCommand(id))))
            .RequireAuthorization("Admin");
    }
}
