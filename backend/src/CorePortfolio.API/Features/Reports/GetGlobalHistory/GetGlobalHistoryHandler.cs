using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Performance;

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

        return snapshots
            .GroupBy(snapshot => snapshot.Date.Date)
            .OrderBy(group => group.Key)
            .Select(group => new SnapshotDto(
                group.Key.ToString("yyyy-MM-dd"),
                group.Sum(snapshot => snapshot.TotalInvested),
                group.Sum(snapshot => snapshot.HoldingsValue),
                group.Sum(snapshot => snapshot.HoldingsValue),
                group.Sum(snapshot => snapshot.CashValue),
                group.Sum(snapshot => snapshot.NetAssetValue),
                group.Sum(snapshot => snapshot.NetExternalFlow),
                group.Sum(snapshot => snapshot.RealizedPnl),
                group.Sum(snapshot => snapshot.UnrealizedPnl),
                group.Sum(snapshot => snapshot.Income),
                group.Sum(snapshot => snapshot.Fees),
                "VND",
                group.Max(snapshot => snapshot.UsdToVndRate),
                group.Max(snapshot => snapshot.ValuationTimestamp),
                AggregateQualityStatus(group.Select(snapshot => snapshot.QualityStatus)),
                group.Sum(snapshot => snapshot.StaleAssetCount),
                group.Sum(snapshot => snapshot.UnclassifiedCashFlowCount)))
            .ToList();
    }

    private static string AggregateQualityStatus(IEnumerable<string> statuses)
    {
        var values = statuses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (values.Contains(PortfolioSnapshotQuality.Partial))
            return PortfolioSnapshotQuality.Partial;
        if (values.Contains(PortfolioSnapshotQuality.Legacy))
            return PortfolioSnapshotQuality.Legacy;
        if (values.Contains(PortfolioSnapshotQuality.StalePrices))
            return PortfolioSnapshotQuality.StalePrices;
        return PortfolioSnapshotQuality.Complete;
    }
}
