using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public static class TakeDailySnapshotEndpoint
{
    public static void MapTakeDailySnapshotEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports/snapshots/trigger", async (IMediator mediator) =>
        {
            await mediator.Send(new TakeDailySnapshotCommand());
            return Results.Ok(new { Message = "Snapshot triggered successfully." });
        })
        .WithName("TakeDailySnapshot")
        .WithTags("Reports")
        .Produces(StatusCodes.Status200OK);
    }
}
