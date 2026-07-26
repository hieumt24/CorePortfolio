using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;

public record UpdateMarketAssetPriceRequest(decimal NewPrice);

public static class UpdateMarketAssetPriceEndpoint
{
    public static void MapUpdateMarketAssetPriceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/market-assets/{id:guid}/price", async (Guid id, [FromBody] UpdateMarketAssetPriceRequest request, IMediator mediator) =>
        {
            var command = new UpdateMarketAssetPriceCommand(id, request.NewPrice);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithName("UpdateMarketAssetPrice")
        .WithTags("Assets")
        .RequireAuthorization(AdminPermissionCatalog.MarketDataManage);
    }
}
