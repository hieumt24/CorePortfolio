using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Reports.GetGlobalHistory;

public static class GetGlobalHistoryEndpoint
{
    public static void MapGetGlobalHistoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/global-history", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetGlobalHistoryQuery());
            return Results.Ok(result);
        })
        .WithName("GetGlobalHistory")
        .WithTags("Reports")
        .Produces<List<SnapshotDto>>(StatusCodes.Status200OK);
    }
}
