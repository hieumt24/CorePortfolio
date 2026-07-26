using CorePortfolio.Domain.Performance;
using MediatR;

namespace CorePortfolio.API.Features.Performance.GetDrawdownSeries;

public sealed record GetDrawdownSeriesQuery(
    Guid? PortfolioId,
    string AssetGroup,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<PerformanceDrawdownSeriesDto>;

public sealed class GetDrawdownSeriesHandler(PerformanceDataService dataService)
    : IRequestHandler<GetDrawdownSeriesQuery, PerformanceDrawdownSeriesDto>
{
    public async Task<PerformanceDrawdownSeriesDto> Handle(
        GetDrawdownSeriesQuery request,
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
        var result = DrawdownCalculator.Calculate(data.ToPerformancePoints());

        return new PerformanceDrawdownSeriesDto(
            data.From,
            data.To,
            result.MaximumDrawdown.ToMetric(100m),
            result.Points.Select(point => new PerformanceDrawdownPointDto(
                point.Date.ToString("yyyy-MM-dd"),
                point.GrowthIndex,
                point.PeakGrowthIndex,
                point.Drawdown * 100m)).ToList(),
            data.Quality);
    }
}
