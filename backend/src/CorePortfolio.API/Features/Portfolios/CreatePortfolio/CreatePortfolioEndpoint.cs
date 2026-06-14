using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Portfolios.CreatePortfolio;

public record CreatePortfolioRequest(string Name, string Description);

public static class CreatePortfolioEndpoint
{
    public static void MapCreatePortfolioEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/portfolios", async ([FromBody] CreatePortfolioRequest request, IMediator mediator) =>
        {
            var command = new CreatePortfolioCommand(request.Name, request.Description);
            var id = await mediator.Send(command);
            return Results.Created($"/api/portfolios/{id}", new { Id = id });
        })
        .WithName("CreatePortfolio")
        .WithTags("Portfolios");
    }
}
