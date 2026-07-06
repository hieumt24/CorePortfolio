using CorePortfolio.API.Features.Reports.GetGlobalReport;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;

public class GetRebalanceSuggestionsHandler : IRequestHandler<GetRebalanceSuggestionsQuery, List<RebalanceSuggestionDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly ExchangeRateService _exchangeRateService;

    public GetRebalanceSuggestionsHandler(AppDbContext dbContext, ICurrentUserService currentUserService, IMediator mediator, ExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<List<RebalanceSuggestionDto>> Handle(GetRebalanceSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException();

        var report = await _mediator.Send(new GetGlobalReportQuery(userId.Value), cancellationToken);

        var vndUsdRate = await _exchangeRateService.GetUsdToVndAsync(cancellationToken);

        var convertedAllocations = new List<(string CategoryName, decimal TotalValue)>();
        foreach (var cat in report.AllocationsByCategory)
        {
            var currentVal = cat.CurrentValue;
            if (request.Currency == "VND" && cat.Currency == "USD") currentVal *= vndUsdRate;
            else if (request.Currency == "USD" && cat.Currency == "VND") currentVal /= vndUsdRate;

            convertedAllocations.Add((cat.CategoryName, currentVal));
        }

        var groupedAllocations = convertedAllocations
            .GroupBy(c => c.CategoryName)
            .Select(g => new
            {
                CategoryName = g.Key,
                TotalValue = g.Sum(c => c.TotalValue)
            }).ToList();

        var totalPortfolioValue = groupedAllocations.Sum(c => c.TotalValue);

        var targets = await _dbContext.TargetAllocations
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);

        var allCategories = await _dbContext.AssetCategories.ToListAsync(cancellationToken);

        var suggestions = new List<RebalanceSuggestionDto>();

        foreach (var cat in allCategories)
        {
            var currentObj = groupedAllocations.FirstOrDefault(g => g.CategoryName == cat.Name);
            var currentVal = currentObj?.TotalValue ?? 0m;

            var targetObj = targets.FirstOrDefault(t => t.CategoryId == cat.Id);
            var targetPct = targetObj?.TargetPercentage ?? 0m;

            var targetVal = totalPortfolioValue * (targetPct / 100m);
            var diffVal = targetVal - currentVal;

            // Only suggest if the difference is more than 0.1% of portfolio value
            if (Math.Abs(diffVal) > totalPortfolioValue * 0.001m)
            {
                suggestions.Add(new RebalanceSuggestionDto
                {
                    CategoryId = cat.Id,
                    CategoryName = cat.Name,
                    CurrentValue = currentVal,
                    TargetValue = targetVal,
                    DifferenceValue = Math.Abs(diffVal),
                    Action = diffVal > 0 ? "Buy" : "Sell"
                });
            }
        }

        return suggestions.OrderByDescending(s => s.DifferenceValue).ToList();
    }
}
