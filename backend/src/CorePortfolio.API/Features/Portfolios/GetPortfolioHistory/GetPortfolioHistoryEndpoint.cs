using CorePortfolio.API.Features.Reports.GetGlobalHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioHistory;

public static class GetPortfolioHistoryEndpoint
{
    public static void MapGetPortfolioHistoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/portfolios/{id:guid}/history", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPortfolioHistoryQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetPortfolioHistory")
        .WithTags("Portfolios")
        .Produces<List<SnapshotDto>>(StatusCodes.Status200OK);
    }
}
