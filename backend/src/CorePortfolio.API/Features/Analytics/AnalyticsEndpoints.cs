using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

using CorePortfolio.API.Services;
namespace CorePortfolio.API.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapGet("/cashflow", async (IMediator mediator, [FromQuery] int months = 6, [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(new GetCashflowAnalyticsQuery(months, currency));
            return Results.Ok(result);
        });

        group.MapGet("/allocation", async (IMediator mediator, [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(new GetAssetAllocationQuery(currency));
            return Results.Ok(result);
        });

        group.MapGet("/performance", async (IMediator mediator, [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(new GetPerformanceAnalyticsQuery(currency));
            return Results.Ok(result);
        });

        group.MapGet("/dividends", async (IMediator mediator, [FromQuery] int months = 12, [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(new GetDividendAnalyticsQuery(months, currency));
            return Results.Ok(result);
        });

        group.MapGet("/target-allocations", async (IMediator mediator, ICurrentUserService currentUserService) =>
        {
            if (currentUserService.UserId == null) return Results.Unauthorized();
            var result = await mediator.Send(new GetTargetAllocationsQuery(currentUserService.UserId.Value));
            return Results.Ok(result);
        });

        group.MapGet("/heatmap", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCashflowHeatmapQuery());
            return Results.Ok(result);
        });

        group.MapPost("/target-allocations", async (IMediator mediator, ICurrentUserService currentUserService, [FromBody] List<TargetAllocationInput> inputs) =>
        {
            if (currentUserService.UserId == null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateTargetAllocationsCommand(currentUserService.UserId.Value, inputs));
            return Results.Ok(result);
        });
    }
}
