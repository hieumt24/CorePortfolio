using CorePortfolio.Domain.Performance;
using MediatR;

namespace CorePortfolio.API.Features.Performance.GetMonthlyReturns;

public sealed record GetMonthlyReturnsQuery(
    Guid? PortfolioId,
    string AssetGroup,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<PerformanceMonthlyReturnsDto>;

public sealed class GetMonthlyReturnsHandler(PerformanceDataService dataService)
    : IRequestHandler<GetMonthlyReturnsQuery, PerformanceMonthlyReturnsDto>
{
    public async Task<PerformanceMonthlyReturnsDto> Handle(
        GetMonthlyReturnsQuery request,
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
        var result = MonthlyReturnCalculator.Calculate(data.ToPerformancePoints());

        return new PerformanceMonthlyReturnsDto(
            data.From,
            data.To,
            result.Returns.Select(item => new PerformanceMonthlyReturnDto(
                item.Month.ToString("yyyy-MM"),
                item.Return.HasValue ? item.Return.Value * 100m : null,
                item.Status.ToString(),
                item.Reason)).ToList(),
            result.BestMonth.ToMetric(100m),
            result.WorstMonth.ToMetric(100m),
            result.MonthlyVolatility.ToMetric(100m),
            data.Quality);
    }
}
