using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;
using System.Net;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record CreateMarketAssetRequest(Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null);

public record UpdateMarketAssetRequest(Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null);
public record SyncVn100MarketAssetsRequest(Guid CategoryId);
public record SyncFundMarketAssetsRequest(Guid CategoryId);

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

        group.MapGet("/", async (
            IMediator mediator,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] string? priceSource,
            [FromQuery] string? priceStatus,
            [FromQuery] string sortBy = "symbol",
            [FromQuery] string sortDirection = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var result = await mediator.Send(new GetMarketAssetsQuery(
                categoryId,
                search,
                priceSource,
                priceStatus,
                sortBy,
                sortDirection,
                page,
                pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/coingecko-price/{coinId}", async (string coinId, IMediator mediator) =>
        {
            var price = await mediator.Send(new GetCoinGeckoPriceQuery(coinId));
            return price.HasValue ? Results.Ok(new { Price = price.Value }) : Results.NotFound();
        });

        group.MapGet("/kbs-price/{symbol}", async (string symbol, IMediator mediator) =>
        {
            try
            {
                var price = await mediator.Send(new GetKbsStockPriceQuery(symbol));
                return price.HasValue
                    ? Results.Ok(new { Price = price.Value })
                    : Results.NotFound(new { message = $"KBS không trả về giá hợp lệ cho mã {symbol.ToUpperInvariant()}." });
            }
            catch (HttpRequestException ex)
            {
                var statusCode = ex.StatusCode == HttpStatusCode.NotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status502BadGateway;
                return Results.Problem(statusCode: statusCode, title: "Không lấy được giá từ KBS", detail: ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Mã chứng khoán không hợp lệ", detail: ex.Message);
            }
        });

        group.MapGet("/kbs-instruments", async ([FromQuery] string? query, IMediator mediator) =>
        {
            var result = await mediator.Send(new SearchKbsInstrumentsQuery { Query = query ?? string.Empty });
            return Results.Ok(result);
        });

        group.MapPost("/sync-vn100", async (
            [FromBody] SyncVn100MarketAssetsRequest request,
            IMediator mediator) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(new SyncVn100MarketAssetsCommand(request.CategoryId)));
            }
            catch (HttpRequestException exception)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Không thể đồng bộ VN100 từ KBS",
                    detail: exception.Message);
            }
        }).RequireAuthorization("Admin");

        group.MapPost("/sync-funds", async (
            [FromBody] SyncFundMarketAssetsRequest request,
            IMediator mediator) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(new SyncFundMarketAssetsCommand(request.CategoryId)));
            }
            catch (HttpRequestException exception)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Không thể đồng bộ chứng chỉ quỹ từ Fmarket",
                    detail: exception.Message);
            }
        }).RequireAuthorization("Admin");

        group.MapPost("/refresh", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new RefreshMarketAssetPricesCommand())))
            .RequireAuthorization("Admin");

        group.MapPost("/{id:guid}/refresh", async (Guid id, IMediator mediator) =>
            Results.Ok(await mediator.Send(new RefreshMarketAssetPricesCommand(id))))
            .RequireAuthorization("Admin");
    }
}
