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
            .ToListAsync(cancellationToken);

        return snapshots.GroupBy(s => s.Date.Date).OrderBy(g => g.Key).Select(g => new SnapshotDto(
            g.Key.ToString("yyyy-MM-dd"), g.Sum(s => s.TotalInvested), g.Sum(s => s.TotalValue), "VND",
            g.Max(s => s.UsdToVndRate), g.Max(s => s.ValuationTimestamp),
            g.All(s => s.QualityStatus == "Complete") ? "Complete" : "Partial"
        )).ToList();
    }
}
