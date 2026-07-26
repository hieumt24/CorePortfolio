using CorePortfolio.Domain.Performance;
using MediatR;

namespace CorePortfolio.API.Features.Performance.GetPerformanceSummary;

public sealed record GetPerformanceSummaryQuery(
    Guid? PortfolioId,
    string AssetGroup,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<PerformanceSummaryDto>;

public sealed class GetPerformanceSummaryHandler(PerformanceDataService dataService)
    : IRequestHandler<GetPerformanceSummaryQuery, PerformanceSummaryDto>
{
    public async Task<PerformanceSummaryDto> Handle(
        GetPerformanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var data = await dataService.LoadAsync(
            new PerformanceRequest(
                request.PortfolioId,
                request.AssetGroup,
                request.From,
                request.To,
                request.Currency),
            cancellationToken);
        var points = data.ToPerformancePoints();
        var absoluteReturn = TimeWeightedReturnCalculator.CalculateAbsoluteReturn(points);
        var twr = TimeWeightedReturnCalculator.Calculate(points);
        var xirr = MoneyWeightedReturnCalculator.CalculateFromPerformancePoints(points);
        var drawdown = DrawdownCalculator.Calculate(points);
        var monthly = MonthlyReturnCalculator.Calculate(points);
        var first = data.Snapshots.FirstOrDefault();
        var last = data.Snapshots.LastOrDefault();

        return new PerformanceSummaryDto(
            data.Currency,
            data.From,
            data.To,
            first?.NetAssetValue ?? 0,
            last?.NetAssetValue ?? 0,
            data.Snapshots.Sum(snapshot => snapshot.NetExternalFlow),
            absoluteReturn.ToMetric(),
            twr.TotalReturn.ToMetric(100m),
            xirr.ToMetric(100m),
            last?.RealizedPnl ?? 0,
            last?.UnrealizedPnl ?? 0,
            (last?.RealizedPnl ?? 0) + (last?.UnrealizedPnl ?? 0),
            drawdown.MaximumDrawdown.ToMetric(100m),
            monthly.BestMonth.ToMetric(100m),
            monthly.WorstMonth.ToMetric(100m),
            monthly.MonthlyVolatility.ToMetric(100m),
            data.Quality);
    }
}
