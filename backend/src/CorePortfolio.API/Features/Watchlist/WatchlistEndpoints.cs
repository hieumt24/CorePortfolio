using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CorePortfolio.API.Features.Watchlist;

public static class WatchlistEndpoints
{
    public static void MapWatchlistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/watchlist").WithTags("Watchlist");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWatchlistQuery());
            return Results.Ok(result);
        });

        group.MapPost("/", async (AddToWatchlistCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(new { Id = result });
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new RemoveFromWatchlistCommand(id));
            return Results.NoContent();
        });
    }
}
