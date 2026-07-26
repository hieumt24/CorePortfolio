using CorePortfolio.API.Common;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Performance;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Performance.Benchmarks;

public sealed record UpsertBenchmarkPricePointCommand(
    Guid BenchmarkId,
    DateTime Date,
    decimal ClosePrice,
    string? Source) : IRequest<bool>;

public sealed class UpsertBenchmarkPricePointHandler(AppDbContext dbContext)
    : IRequestHandler<UpsertBenchmarkPricePointCommand, bool>
{
    public async Task<bool> Handle(
        UpsertBenchmarkPricePointCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ClosePrice <= 0)
            throw new RequestValidationException("Giá benchmark phải lớn hơn 0.");

        var benchmark = await dbContext.BenchmarkDefinitions
            .SingleOrDefaultAsync(item => item.Id == request.BenchmarkId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy benchmark.");
        var date = request.Date.Date;
        var pricePoint = await dbContext.BenchmarkPricePoints
            .SingleOrDefaultAsync(item =>
                item.BenchmarkDefinitionId == benchmark.Id &&
                item.Date == date,
                cancellationToken);
        if (pricePoint is null)
        {
            pricePoint = new BenchmarkPricePoint
            {
                Id = Guid.NewGuid(),
                BenchmarkDefinitionId = benchmark.Id,
                Date = date
            };
            dbContext.BenchmarkPricePoints.Add(pricePoint);
        }

        pricePoint.ClosePrice = request.ClosePrice;
        pricePoint.Currency = benchmark.Currency;
        pricePoint.Source = string.IsNullOrWhiteSpace(request.Source)
            ? "Manual"
            : request.Source.Trim();
        pricePoint.QualityStatus = PortfolioSnapshotQuality.Complete;
        pricePoint.CapturedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
