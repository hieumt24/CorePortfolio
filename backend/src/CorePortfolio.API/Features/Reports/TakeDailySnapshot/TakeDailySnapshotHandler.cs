using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public class TakeDailySnapshotHandler : IRequestHandler<TakeDailySnapshotCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;

    public TakeDailySnapshotHandler(AppDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<bool> Handle(TakeDailySnapshotCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var portfolios = await _dbContext.Portfolios.Select(p => p.Id).ToListAsync(cancellationToken);

        foreach (var pId in portfolios)
        {
            // Check if snapshot exists for today
            var existingSnapshot = await _dbContext.PortfolioSnapshots
                .FirstOrDefaultAsync(s => s.PortfolioId == pId && s.Date == today, cancellationToken);
            
            // Get summary
            var summary = await _mediator.Send(new GetPortfolioSummaryQuery(pId), cancellationToken);
            if (summary == null)
                continue;

            if (existingSnapshot != null)
            {
                existingSnapshot.TotalInvested = summary.TotalInvested;
                existingSnapshot.TotalValue = summary.CurrentTotalValue;
            }
            else
            {
                var snapshot = new PortfolioSnapshot
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = pId,
                    Date = today,
                    TotalInvested = summary.TotalInvested,
                    TotalValue = summary.CurrentTotalValue
                };
                _dbContext.PortfolioSnapshots.Add(snapshot);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
