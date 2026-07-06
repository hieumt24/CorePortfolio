using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Reports.GetGlobalReport;

public class GetGlobalReportHandler : IRequestHandler<GetGlobalReportQuery, GlobalReportDto>
{
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;

    public GetGlobalReportHandler(AppDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<GlobalReportDto> Handle(GetGlobalReportQuery request, CancellationToken cancellationToken)
    {
        var portfolios = await _dbContext.Portfolios.AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);
        var categories = new Dictionary<string, CategoryAllocationDto>();
        var portfolioAllocations = new List<PortfolioAllocationDto>();

        foreach (var portfolio in portfolios)
        {
            var summary = await _mediator.Send(new GetPortfolioSummaryQuery(portfolio.Id, request.UserId), cancellationToken);
            if (summary == null) continue;

            foreach (var asset in summary.Assets)
            {
                var key = $"{asset.CategoryName}_{asset.Currency}";
                categories.TryGetValue(key, out var existing);
                categories[key] = new CategoryAllocationDto(asset.CategoryName, asset.Currency,
                    (existing?.TotalInvested ?? 0) + asset.TotalCost,
                    (existing?.CurrentValue ?? 0) + asset.CurrentValue);
            }

            foreach (var cash in summary.CashBalances)
            {
                var key = $"Fiat_{cash.Currency}";
                categories.TryGetValue(key, out var existing);
                categories[key] = new CategoryAllocationDto("Fiat", cash.Currency,
                    existing?.TotalInvested ?? 0, (existing?.CurrentValue ?? 0) + cash.Balance);
            }

            var currencyRows = summary.Assets.GroupBy(a => a.Currency)
                .Select(g => new PortfolioCurrencyAllocationDto(g.Key, g.Sum(a => a.TotalCost),
                    g.Sum(a => a.CurrentValue) + summary.CashBalances.Where(c => c.Currency == g.Key).Sum(c => c.Balance)))
                .ToList();
            foreach (var cash in summary.CashBalances.Where(c => currencyRows.All(r => r.Currency != c.Currency)))
                currencyRows.Add(new PortfolioCurrencyAllocationDto(cash.Currency, 0, cash.Balance));

            portfolioAllocations.Add(new PortfolioAllocationDto(portfolio.Id, portfolio.Name, currencyRows));
        }

        return new GlobalReportDto(categories.Values.ToList(), portfolioAllocations);
    }
}
