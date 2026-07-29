using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Reports.GetGlobalReport;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Analytics;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;

public sealed class GetRebalanceSuggestionsHandler
    : IRequestHandler<GetRebalanceSuggestionsQuery, RebalanceAssessmentDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly ExchangeRateService _exchangeRateService;
    private readonly RebalancingOptions _options;

    public GetRebalanceSuggestionsHandler(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IMediator mediator,
        ExchangeRateService exchangeRateService,
        IOptions<RebalancingOptions> options)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _exchangeRateService = exchangeRateService;
        _options = options.Value;
    }

    public async Task<RebalanceAssessmentDto> Handle(
        GetRebalanceSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var currency = request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD"))
            throw new RequestValidationException("Currency phải là VND hoặc USD.");

        var targets = await _dbContext.TargetAllocations
            .AsNoTracking()
            .Where(target => target.UserId == userId)
            .ToListAsync(cancellationToken);
        var targetAssessment = TargetAllocationPolicy.Evaluate(
            targets.Select(target =>
                new TargetAllocationWeight(
                    target.CategoryId,
                    target.TargetPercentage)));
        if (!targetAssessment.IsActionable)
        {
            return CreateAssessment(
                targetAssessment,
                false,
                targetAssessment.Reason,
                []);
        }

        var report = await _mediator.Send(
            new GetGlobalReportQuery(userId.Value),
            cancellationToken);
        var vndUsdRate = await _exchangeRateService.GetUsdToVndAsync(cancellationToken);
        var convertedAllocations = new List<(string CategoryName, decimal TotalValue)>();
        foreach (var allocation in report.AllocationsByCategory)
        {
            var currentValue = allocation.CurrentValue;
            if (currency == "VND" && allocation.Currency == "USD")
                currentValue *= vndUsdRate;
            else if (currency == "USD" && allocation.Currency == "VND")
                currentValue /= vndUsdRate;

            convertedAllocations.Add((allocation.CategoryName, currentValue));
        }

        var groupedAllocations = convertedAllocations
            .GroupBy(allocation => allocation.CategoryName)
            .Select(group => new
            {
                CategoryName = group.Key,
                TotalValue = group.Sum(allocation => allocation.TotalValue)
            })
            .ToList();
        var totalPortfolioValue = groupedAllocations.Sum(allocation => allocation.TotalValue);
        if (totalPortfolioValue <= 0m)
        {
            return CreateAssessment(
                targetAssessment,
                false,
                "Chưa có giá trị danh mục để đánh giá tái cân bằng.",
                []);
        }

        var categories = await _dbContext.AssetCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var suggestions = new List<RebalanceSuggestionDto>();
        foreach (var category in categories)
        {
            var currentValue = groupedAllocations
                .FirstOrDefault(allocation => allocation.CategoryName == category.Name)
                ?.TotalValue ?? 0m;
            var targetPercentage = targets
                .FirstOrDefault(target => target.CategoryId == category.Id)
                ?.TargetPercentage ?? 0m;
            var currentPercentage = currentValue / totalPortfolioValue * 100m;
            if (!TargetAllocationPolicy.IsOutsideTolerance(
                    currentPercentage,
                    targetPercentage,
                    _options.TolerancePercentagePoints))
            {
                continue;
            }

            var targetValue = totalPortfolioValue * targetPercentage / 100m;
            var differenceValue = targetValue - currentValue;
            suggestions.Add(new RebalanceSuggestionDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                CurrentValue = currentValue,
                TargetValue = targetValue,
                DifferenceValue = Math.Abs(differenceValue),
                Action = differenceValue > 0m ? "Increase" : "Reduce"
            });
        }

        return CreateAssessment(
            targetAssessment,
            true,
            suggestions.Count == 0
                ? $"Danh mục đang nằm trong biên dung sai {_options.TolerancePercentagePoints:0.##} điểm phần trăm."
                : null,
            suggestions
                .OrderByDescending(suggestion => suggestion.DifferenceValue)
                .ToList());
    }

    private RebalanceAssessmentDto CreateAssessment(
        TargetAllocationPlanAssessment targetAssessment,
        bool isActionable,
        string? reason,
        IReadOnlyList<RebalanceSuggestionDto> suggestions) =>
        new(
            targetAssessment.Status,
            targetAssessment.TotalPercentage,
            _options.TolerancePercentagePoints,
            isActionable,
            reason,
            suggestions);
}
