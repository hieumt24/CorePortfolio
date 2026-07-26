using CorePortfolio.API.Common;
using CorePortfolio.Domain.Performance;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Performance.Benchmarks;

public sealed record GetBenchmarkComparisonQuery(
    Guid BenchmarkId,
    Guid? PortfolioId,
    string AssetGroup,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<BenchmarkComparisonDto>;

public sealed class GetBenchmarkComparisonHandler(
    AppDbContext dbContext,
    PerformanceDataService dataService)
    : IRequestHandler<GetBenchmarkComparisonQuery, BenchmarkComparisonDto>
{
    public async Task<BenchmarkComparisonDto> Handle(
        GetBenchmarkComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var benchmark = await dbContext.BenchmarkDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == request.BenchmarkId &&
                item.IsActive,
                cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy benchmark đang hoạt động.");
        var data = await dataService.LoadAsync(
            new PerformanceRequest(
                request.PortfolioId,
                request.AssetGroup,
                request.From,
                request.To,
                request.Currency),
            cancellationToken);
        var benchmarkPoints = await dbContext.BenchmarkPricePoints
            .AsNoTracking()
            .Where(point =>
                point.BenchmarkDefinitionId == benchmark.Id &&
                point.Date >= data.From &&
                point.Date < data.To.AddDays(1))
            .OrderBy(point => point.Date)
            .ToListAsync(cancellationToken);

        var twr = TimeWeightedReturnCalculator.Calculate(data.ToPerformancePoints());
        var portfolioGrowth = new Dictionary<DateTime, decimal>();
        if (data.Snapshots.Count > 0)
            portfolioGrowth[data.Snapshots[0].Date.Date] = 100m;
        foreach (var period in twr.Periods)
            portfolioGrowth[period.Date.Date] = period.GrowthIndex;

        var benchmarkByDate = benchmarkPoints.ToDictionary(
            point => point.Date.Date,
            point => point);
        var commonDate = portfolioGrowth.Keys
            .Where(benchmarkByDate.ContainsKey)
            .OrderBy(date => date)
            .FirstOrDefault();
        var hasCommonDate = commonDate != default;
        var portfolioBase = hasCommonDate ? portfolioGrowth[commonDate] : 100m;
        var benchmarkBase = hasCommonDate ? benchmarkByDate[commonDate].ClosePrice : 0m;

        var points = portfolioGrowth
            .Where(item => !hasCommonDate || item.Key >= commonDate)
            .OrderBy(item => item.Key)
            .Select(item =>
            {
                var hasBenchmark = benchmarkByDate.TryGetValue(item.Key, out var benchmarkPoint);
                return new BenchmarkComparisonPointDto(
                    item.Key.ToString("yyyy-MM-dd"),
                    portfolioBase == 0 ? 100m : item.Value / portfolioBase * 100m,
                    hasBenchmark && benchmarkBase > 0
                        ? benchmarkPoint!.ClosePrice / benchmarkBase * 100m
                        : null,
                    !hasBenchmark);
            })
            .ToList();
        var missingBenchmarkDays = points.Count(point => point.HasBenchmarkGap);
        var benchmarkHasStalePrices = benchmarkPoints.Any(point =>
            point.QualityStatus == PortfolioSnapshotQuality.StalePrices);
        var qualityStatus = !hasCommonDate || missingBenchmarkDays > 0
            ? PortfolioSnapshotQuality.Partial
            : benchmarkHasStalePrices
                ? PortfolioSnapshotQuality.StalePrices
                : PortfolioSnapshotQuality.Complete;

        return new BenchmarkComparisonDto(
            benchmark.Id,
            benchmark.Name,
            benchmark.Symbol,
            benchmark.Currency,
            hasCommonDate ? commonDate.ToString("yyyy-MM-dd") : null,
            points,
            missingBenchmarkDays,
            qualityStatus,
            data.Quality);
    }
}
