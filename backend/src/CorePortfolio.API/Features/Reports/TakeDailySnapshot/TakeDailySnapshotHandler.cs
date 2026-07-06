using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public class TakeDailySnapshotHandler : IRequestHandler<TakeDailySnapshotCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ExchangeRateService _exchangeRateService;

    public TakeDailySnapshotHandler(AppDbContext dbContext, IMediator mediator, ExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<bool> Handle(TakeDailySnapshotCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var portfolios = await _dbContext.Portfolios.Select(p => new { p.Id, p.UserId }).ToListAsync(cancellationToken);
        var usdToVnd = await _exchangeRateService.GetUsdToVndAsync(cancellationToken);

        foreach (var portfolio in portfolios)
        {
            // Check if snapshot exists for today
            var existingSnapshot = await _dbContext.PortfolioSnapshots
                .FirstOrDefaultAsync(s => s.PortfolioId == portfolio.Id && s.Date == today, cancellationToken);
            
            // Get summary
            var summary = await _mediator.Send(new GetPortfolioSummaryQuery(portfolio.Id, portfolio.UserId), cancellationToken);
            if (summary == null)
                continue;

            if (existingSnapshot != null)
            {
                existingSnapshot.TotalInvested = summary.TotalInvested;
                existingSnapshot.TotalValue = summary.CurrentTotalValue;
                existingSnapshot.UsdToVndRate = usdToVnd;
                existingSnapshot.ValuationTimestamp = DateTime.UtcNow;
                existingSnapshot.QualityStatus = summary.Assets.Any(a => a.PriceUpdatedAt < DateTime.UtcNow.AddDays(-2)) ? "StalePrices" : "Complete";
            }
            else
            {
                var snapshot = new PortfolioSnapshot
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolio.Id,
                    Date = today,
                    TotalInvested = summary.TotalInvested,
                    TotalValue = summary.CurrentTotalValue,
                    BaseCurrency = "VND",
                    UsdToVndRate = usdToVnd,
                    ValuationTimestamp = DateTime.UtcNow,
                    QualityStatus = summary.Assets.Any(a => a.PriceUpdatedAt < DateTime.UtcNow.AddDays(-2)) ? "StalePrices" : "Complete"
                };
                _dbContext.PortfolioSnapshots.Add(snapshot);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
