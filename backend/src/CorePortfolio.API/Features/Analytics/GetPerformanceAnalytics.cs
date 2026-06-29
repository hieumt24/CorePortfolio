using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public record GetPerformanceAnalyticsQuery(string Currency = "VND") : IRequest<PerformanceAnalyticsDto>;

public class PerformanceAnalyticsDto
{
    public List<AssetPerformanceDto> TopPerformers { get; set; } = new();
    public List<AssetPerformanceDto> WorstPerformers { get; set; } = new();
    public List<PortfolioHistoryDataPointDto> TotalValueHistory { get; set; } = new();
}

public class AssetPerformanceDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal ReturnPercentage { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal TotalBought { get; set; } // Use for correct grouping math
}

public class PortfolioHistoryDataPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
}

public class GetPerformanceAnalyticsHandler : IRequestHandler<GetPerformanceAnalyticsQuery, PerformanceAnalyticsDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public GetPerformanceAnalyticsHandler(AppDbContext dbContext, ICurrentUserService currentUserService, IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<PerformanceAnalyticsDto> Handle(GetPerformanceAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException();

        var portfolios = await _dbContext.Portfolios
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        var assetPerformances = new List<AssetPerformanceDto>();

        var exchangeRateSetting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "VndUsdRate", cancellationToken);
        decimal vndUsdRate = 25400m;
        if (exchangeRateSetting != null && decimal.TryParse(exchangeRateSetting.Value, out var rate))
            vndUsdRate = rate;

        foreach (var p in portfolios)
        {
            var summary = await _mediator.Send(new GetPortfolioSummaryQuery(p.Id), cancellationToken);
            if (summary == null) continue;

            foreach (var asset in summary.Assets.Where(a => a.TotalQuantity > 0 && a.CategoryName != "Fiat"))
            {
                var currentVal = asset.CurrentValue;
                var cost = asset.TotalCost;
                var totalBought = asset.TotalBought;

                if (request.Currency == "VND" && asset.Currency == "USD")
                {
                    currentVal *= vndUsdRate;
                    cost *= vndUsdRate;
                    totalBought *= vndUsdRate;
                }
                else if (request.Currency == "USD" && asset.Currency == "VND")
                {
                    currentVal /= vndUsdRate;
                    cost /= vndUsdRate;
                    totalBought /= vndUsdRate;
                }

                var returnVal = currentVal - cost;
                var returnPct = totalBought > 0 ? (returnVal / totalBought) * 100 : 0;

                assetPerformances.Add(new AssetPerformanceDto
                {
                    Symbol = asset.Symbol,
                    Name = asset.Name,
                    ReturnPercentage = returnPct,
                    ReturnValue = returnVal,
                    TotalBought = totalBought
                });
            }
        }

        // Aggregate by symbol
        var groupedPerformances = assetPerformances
            .GroupBy(a => a.Symbol)
            .Select(g => 
            {
                var totalReturn = g.Sum(a => a.ReturnValue);
                var totalBoughtSum = g.Sum(a => a.TotalBought);
                var returnPct = totalBoughtSum > 0 ? (totalReturn / totalBoughtSum) * 100 : 0;

                return new AssetPerformanceDto
                {
                    Symbol = g.Key,
                    Name = g.First().Name,
                    ReturnValue = totalReturn,
                    ReturnPercentage = returnPct
                };
            })
            .ToList();

        var topPerformers = groupedPerformances.OrderByDescending(a => a.ReturnPercentage).Take(5).ToList();
        var worstPerformers = groupedPerformances.OrderBy(a => a.ReturnPercentage).Take(5).ToList();

        // For history, fetch from PortfolioSnapshots
        var history = await _dbContext.PortfolioSnapshots
            .Include(s => s.Portfolio)
            .Where(h => h.Portfolio!.UserId == userId)
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);

        // Group by day for the chart
        var historyPoints = history
            .GroupBy(h => h.Date.Date)
            .Select(g => 
            {
                // In PortfolioSnapshots, TotalValue is saved. We should really save it by Currency, 
                // but for now let's just use it as is since Snapshot logic might need an overhaul later.
                // Assuming snapshots are saved in VND if user prefers VND.
                // For a more accurate history, we just sum TotalValue.
                return new PortfolioHistoryDataPointDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TotalValue = g.Sum(h => h.TotalValue) 
                };
            })
            .ToList();

        return new PerformanceAnalyticsDto
        {
            TopPerformers = topPerformers,
            WorstPerformers = worstPerformers,
            TotalValueHistory = historyPoints
        };
    }
}
