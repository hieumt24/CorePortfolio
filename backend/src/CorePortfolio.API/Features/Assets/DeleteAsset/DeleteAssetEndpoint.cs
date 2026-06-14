using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Assets.DeleteAsset;

public static class DeleteAssetEndpoint
{
    public static void MapDeleteAssetEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/portfolios/{portfolioId:guid}/assets/{assetId:guid}", async (Guid portfolioId, Guid assetId, IMediator mediator) =>
        {
            var command = new DeleteAssetCommand(portfolioId, assetId);
            var success = await mediator.Send(command);
            
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteAsset")
        .WithTags("Assets");
    }
}
