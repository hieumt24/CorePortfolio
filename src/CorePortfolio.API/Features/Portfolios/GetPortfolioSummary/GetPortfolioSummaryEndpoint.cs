using MediatR;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;

public static class GetPortfolioSummaryEndpoint
{
    public static void MapGetPortfolioSummaryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/portfolios/{id:guid}/summary", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPortfolioSummaryQuery(id));
            
            if (result == null)
                return Results.NotFound();
                
            return Results.Ok(result);
        })
        .WithName("GetPortfolioSummary")
        .WithTags("Portfolios");
    }
}
