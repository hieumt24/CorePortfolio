using CorePortfolio.API.Features.Reports.GetGlobalHistory;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioHistory;

public class GetPortfolioHistoryHandler : IRequestHandler<GetPortfolioHistoryQuery, List<SnapshotDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetPortfolioHistoryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<SnapshotDto>> Handle(GetPortfolioHistoryQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.PortfolioSnapshots
            .AsNoTracking()
            .Where(s => s.PortfolioId == request.PortfolioId && s.Portfolio != null && s.Portfolio.UserId == _currentUserService.UserId)
            .OrderBy(s => s.Date)
            .ToListAsync(cancellationToken);

        return snapshots.Select(s => new SnapshotDto(
            s.Date.ToString("yyyy-MM-dd"),
            s.TotalInvested,
            s.NetAssetValue,
            s.HoldingsValue,
            s.CashValue,
            s.NetAssetValue,
            s.NetExternalFlow,
            s.RealizedPnl,
            s.UnrealizedPnl,
            s.Income,
            s.Fees,
            s.BaseCurrency,
            s.UsdToVndRate,
            s.ValuationTimestamp,
            s.QualityStatus,
            s.StaleAssetCount,
            s.UnclassifiedCashFlowCount
        )).ToList();
    }
}
