using CorePortfolio.API.Features.Performance.GetPerformanceDataQuality;
using CorePortfolio.API.Features.Performance.GetPerformanceSummary;
using CorePortfolio.API.Features.Performance.GetPerformanceSeries;
using CorePortfolio.API.Features.Performance.GetDrawdownSeries;
using CorePortfolio.API.Features.Performance.GetMonthlyReturns;
using CorePortfolio.API.Features.Performance.Benchmarks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Performance;

public sealed record UpsertBenchmarkRequest(
    string Name,
    string Symbol,
    Guid? MarketAssetId,
    string AssetGroup,
    bool IsDefault,
    string Currency,
    bool IsActive);

public sealed record UpsertBenchmarkPricePointRequest(
    DateTime Date,
    decimal ClosePrice,
    string? Source);

public static class PerformanceEndpoints
{
    public static void MapPerformanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/performance")
            .WithTags("Performance");

        group.MapGet("/data-quality", async (
            ISender sender,
            [FromQuery] Guid? portfolioId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new GetPerformanceDataQualityQuery(portfolioId, from, to),
                cancellationToken)));

        group.MapGet("/summary", async (
            ISender sender,
            [FromQuery] Guid? portfolioId,
            [FromQuery] string assetGroup = "All",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string currency = "VND",
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetPerformanceSummaryQuery(
                    portfolioId,
                    assetGroup,
                    from,
                    to,
                    currency),
                cancellationToken)));

        group.MapGet("/series", async (
            ISender sender,
            [FromQuery] Guid? portfolioId,
            [FromQuery] string assetGroup = "All",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string currency = "VND",
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetPerformanceSeriesQuery(
                    portfolioId,
                    assetGroup,
                    from,
                    to,
                    currency),
                cancellationToken)));

        group.MapGet("/drawdowns", async (
            ISender sender,
            [FromQuery] Guid? portfolioId,
            [FromQuery] string assetGroup = "All",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string currency = "VND",
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetDrawdownSeriesQuery(
                    portfolioId,
                    assetGroup,
                    from,
                    to,
                    currency),
                cancellationToken)));

        group.MapGet("/monthly-returns", async (
            ISender sender,
            [FromQuery] Guid? portfolioId,
            [FromQuery] string assetGroup = "All",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string currency = "VND",
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetMonthlyReturnsQuery(
                    portfolioId,
                    assetGroup,
                    from,
                    to,
                    currency),
                cancellationToken)));

        group.MapGet("/benchmarks", async (
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new GetBenchmarksQuery(),
                cancellationToken)));

        group.MapGet("/benchmark", async (
            ISender sender,
            [FromQuery] Guid benchmarkId,
            [FromQuery] Guid? portfolioId,
            [FromQuery] string assetGroup = "All",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string currency = "VND",
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetBenchmarkComparisonQuery(
                    benchmarkId,
                    portfolioId,
                    assetGroup,
                    from,
                    to,
                    currency),
                cancellationToken)));

        group.MapPost("/benchmarks", async (
            ISender sender,
            [FromBody] UpsertBenchmarkRequest request,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new UpsertBenchmarkCommand(
                    null,
                    request.Name,
                    request.Symbol,
                    request.MarketAssetId,
                    request.AssetGroup,
                    request.IsDefault,
                    request.Currency,
                    request.IsActive),
                cancellationToken)))
            .RequireAuthorization(AdminPermissionCatalog.MarketDataManage);

        group.MapPut("/benchmarks/{id:guid}", async (
            Guid id,
            ISender sender,
            [FromBody] UpsertBenchmarkRequest request,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new UpsertBenchmarkCommand(
                    id,
                    request.Name,
                    request.Symbol,
                    request.MarketAssetId,
                    request.AssetGroup,
                    request.IsDefault,
                    request.Currency,
                    request.IsActive),
                cancellationToken)))
            .RequireAuthorization(AdminPermissionCatalog.MarketDataManage);

        group.MapPut("/benchmarks/{id:guid}/prices", async (
            Guid id,
            ISender sender,
            [FromBody] UpsertBenchmarkPricePointRequest request,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new UpsertBenchmarkPricePointCommand(
                    id,
                    request.Date,
                    request.ClosePrice,
                    request.Source),
                cancellationToken)))
            .RequireAuthorization(AdminPermissionCatalog.MarketDataManage);
    }
}
