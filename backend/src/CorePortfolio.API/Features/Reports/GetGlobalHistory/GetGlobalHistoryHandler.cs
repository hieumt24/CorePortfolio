using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Reports.GetGlobalHistory;

public class GetGlobalHistoryHandler : IRequestHandler<GetGlobalHistoryQuery, List<SnapshotDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetGlobalHistoryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<SnapshotDto>> Handle(GetGlobalHistoryQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.PortfolioSnapshots
            .AsNoTracking()
            .Where(s => s.Portfolio != null && s.Portfolio.UserId == _currentUserService.UserId)
            .GroupBy(s => s.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalInvested = g.Sum(s => s.TotalInvested),
                TotalValue = g.Sum(s => s.TotalValue)
            })
            .OrderBy(s => s.Date)
            .ToListAsync(cancellationToken);

        return snapshots.Select(s => new SnapshotDto(
            s.Date.ToString("yyyy-MM-dd"),
            s.TotalInvested,
            s.TotalValue
        )).ToList();
    }
}
