using CorePortfolio.Domain.Interfaces;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;

public static class GetRebalanceSuggestionsEndpoint
{
    public static void MapGetRebalanceSuggestionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rebalancing/suggestions", async ([FromQuery] string currency, ICurrentUserService currentUserService, IMediator mediator) =>
        {
            var userId = currentUserService.UserId;
            if (userId == null || userId == Guid.Empty) return Results.Unauthorized();

            var query = new GetRebalanceSuggestionsQuery(userId.Value, currency ?? "VND");
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetRebalanceSuggestions")
        .WithTags("Rebalancing")
        .Produces<List<RebalanceSuggestionDto>>(StatusCodes.Status200OK);
    }
}
