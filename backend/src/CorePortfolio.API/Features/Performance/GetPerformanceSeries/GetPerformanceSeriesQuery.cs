using CorePortfolio.Domain.Performance;
using MediatR;

namespace CorePortfolio.API.Features.Performance.GetPerformanceSeries;

public sealed record GetPerformanceSeriesQuery(
    Guid? PortfolioId,
    string AssetGroup,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<PerformanceSeriesDto>;

public sealed class GetPerformanceSeriesHandler(PerformanceDataService dataService)
    : IRequestHandler<GetPerformanceSeriesQuery, PerformanceSeriesDto>
{
    public async Task<PerformanceSeriesDto> Handle(
        GetPerformanceSeriesQuery request,
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
        var twr = TimeWeightedReturnCalculator.Calculate(data.ToPerformancePoints());
        var periodReturns = twr.Periods.ToDictionary(period => period.Date.Date);
        decimal cumulativeFlow = 0;
        var points = data.Snapshots.Select(snapshot =>
        {
            cumulativeFlow += snapshot.NetExternalFlow;
            periodReturns.TryGetValue(snapshot.Date.Date, out var period);
            return new PerformanceSeriesPointDto(
                snapshot.Date.ToString("yyyy-MM-dd"),
                snapshot.NetAssetValue,
                snapshot.NetExternalFlow,
                cumulativeFlow,
                period?.Return * 100m,
                period?.GrowthIndex ?? 100m,
                snapshot.QualityStatus);
        }).ToList();

        return new PerformanceSeriesDto(
            data.Currency,
            data.From,
            data.To,
            points,
            data.Quality);
    }
}
