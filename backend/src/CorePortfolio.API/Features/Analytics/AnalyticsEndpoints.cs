using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using CorePortfolio.API.Features.Analytics.GetAnalyticsInsights;
using CorePortfolio.API.Features.Analytics.EvaluateAnalyticsScenario;
using CorePortfolio.API.Features.Analytics.DecisionJournal;

namespace CorePortfolio.API.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapGet("/overview", async (
            IMediator mediator,
            [FromQuery] Guid? portfolioId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(
                new GetAnalyticsOverviewQuery(portfolioId, from, to, currency));
            return Results.Ok(result);
        });

        group.MapGet("/insights", async (
            IMediator mediator,
            [FromQuery] Guid? portfolioId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(
                new GetAnalyticsInsightsQuery(portfolioId, from, to, currency));
            return Results.Ok(result);
        });

        group.MapPost("/scenarios/evaluate", async (
            IMediator mediator,
            [FromBody] EvaluateAnalyticsScenarioRequest request) =>
        {
            var result = await mediator.Send(
                new EvaluateAnalyticsScenarioQuery(request));
            return Results.Ok(result);
        });

        group.MapGet("/decisions", async (
            IMediator mediator,
            [FromQuery] Guid? portfolioId,
            [FromQuery] string? status) =>
        {
            var result = await mediator.Send(
                new GetAnalyticsDecisionsQuery(portfolioId, status));
            return Results.Ok(result);
        });

        group.MapPost("/decisions", async (
            IMediator mediator,
            [FromBody] CreateAnalyticsDecisionRequest request) =>
        {
            var result = await mediator.Send(
                new CreateAnalyticsDecisionCommand(request));
            return Results.Ok(result);
        });

        group.MapPut("/decisions/{id:guid}/review", async (
            Guid id,
            IMediator mediator,
            [FromBody] ReviewAnalyticsDecisionRequest request) =>
        {
            var result = await mediator.Send(
                new ReviewAnalyticsDecisionCommand(id, request));
            return Results.Ok(result);
        });

        group.MapGet("/cashflow", async (IMediator mediator, [FromQuery] int months = 6, [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(new GetCashflowAnalyticsQuery(months, currency));
            return Results.Ok(result);
        });

        group.MapGet("/allocation", async (
            IMediator mediator,
            [FromQuery] string currency = "VND",
            [FromQuery] Guid? portfolioId = null) =>
        {
            var result = await mediator.Send(new GetAssetAllocationQuery(currency, portfolioId));
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

        group.MapGet("/target-allocations", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetTargetAllocationsQuery())));

        group.MapGet("/heatmap", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCashflowHeatmapQuery());
            return Results.Ok(result);
        });

        group.MapPost("/target-allocations", async (
            IMediator mediator,
            [FromBody] List<TargetAllocationInput> inputs) =>
        {
            await mediator.Send(new UpdateTargetAllocationsCommand(inputs));
            var result = await mediator.Send(new GetTargetAllocationsQuery());
            return Results.Ok(result);
        });
    }
}
