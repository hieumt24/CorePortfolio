using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Portfolios.UpdatePortfolio;

public static class UpdatePortfolioEndpoint
{
    public static void MapUpdatePortfolioEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/portfolios/{id}", async (Guid id, [FromBody] UpdatePortfolioRequest request, ISender sender) =>
        {
            var command = new UpdatePortfolioCommand(id, request.Name, request.Description);
            var result = await sender.Send(command);
            
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdatePortfolio")
        .WithTags("Portfolios")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}

public record UpdatePortfolioRequest(string Name, string Description);
