using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;

public static class GetRebalanceSuggestionsEndpoint
{
    public static void MapGetRebalanceSuggestionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rebalancing/suggestions", async (
            [FromQuery] string? currency,
            IMediator mediator) =>
            Results.Ok(await mediator.Send(
                new GetRebalanceSuggestionsQuery(currency ?? "VND"))))
        .RequireAuthorization()
        .WithName("GetRebalanceSuggestions")
        .WithTags("Rebalancing")
        .Produces<RebalanceAssessmentDto>(StatusCodes.Status200OK);
    }
}
