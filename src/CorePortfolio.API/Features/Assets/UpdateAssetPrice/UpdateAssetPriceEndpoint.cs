using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Assets.UpdateAssetPrice;

public record UpdateAssetPriceRequest(decimal NewPrice);

public static class UpdateAssetPriceEndpoint
{
    public static void MapUpdateAssetPriceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/assets/{id:guid}/price", async (Guid id, [FromBody] UpdateAssetPriceRequest request, IMediator mediator) =>
        {
            var command = new UpdateAssetPriceCommand(id, request.NewPrice);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithName("UpdateAssetPrice")
        .WithTags("Assets");
    }
}
