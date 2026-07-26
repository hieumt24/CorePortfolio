using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Performance.Benchmarks;

public sealed record GetBenchmarksQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<BenchmarkDefinitionDto>>;

public sealed class GetBenchmarksHandler(AppDbContext dbContext)
    : IRequestHandler<GetBenchmarksQuery, IReadOnlyList<BenchmarkDefinitionDto>>
{
    public async Task<IReadOnlyList<BenchmarkDefinitionDto>> Handle(
        GetBenchmarksQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BenchmarkDefinitions.AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(benchmark => benchmark.IsActive);

        return await query
            .OrderBy(benchmark => benchmark.AssetGroup)
            .ThenByDescending(benchmark => benchmark.IsDefault)
            .ThenBy(benchmark => benchmark.Name)
            .Select(benchmark => new BenchmarkDefinitionDto(
                benchmark.Id,
                benchmark.Name,
                benchmark.Symbol,
                benchmark.MarketAssetId,
                benchmark.AssetGroup,
                benchmark.IsDefault,
                benchmark.Currency,
                benchmark.IsActive,
                benchmark.PricePoints.Count,
                benchmark.PricePoints
                    .OrderByDescending(point => point.Date)
                    .Select(point => (DateTime?)point.Date)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
