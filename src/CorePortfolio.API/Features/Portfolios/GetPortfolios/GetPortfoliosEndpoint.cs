using MediatR;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolios;

public static class GetPortfoliosEndpoint
{
    public static void MapGetPortfoliosEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/portfolios", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPortfoliosQuery());
            return Results.Ok(result);
        })
        .WithName("GetPortfolios")
        .WithTags("Portfolios");
    }
}
