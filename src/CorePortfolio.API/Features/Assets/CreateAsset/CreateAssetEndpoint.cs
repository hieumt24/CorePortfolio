using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Assets.CreateAsset;

public record CreateAssetRequest(Guid MarketAssetId);

public static class CreateAssetEndpoint
{
    public static void MapCreateAssetEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/portfolios/{portfolioId:guid}/assets", async (Guid portfolioId, [FromBody] CreateAssetRequest request, IMediator mediator) =>
        {
            var command = new CreateAssetCommand(portfolioId, request.MarketAssetId);
            var id = await mediator.Send(command);
            return Results.Created($"/api/assets/{id}", new { Id = id });
        })
        .WithName("CreateAsset")
        .WithTags("Assets");
    }
}
