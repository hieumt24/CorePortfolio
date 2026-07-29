using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Dashboard.GetFinancialHealth;
using CorePortfolio.API.Features.DcaPlans;
using CorePortfolio.API.Features.Performance;
using CorePortfolio.API.Features.Performance.GetPerformanceDataQuality;
using CorePortfolio.API.Features.Performance.GetPerformanceSeries;
using CorePortfolio.API.Features.Performance.GetPerformanceSummary;
using CorePortfolio.API.Features.Portfolios.GetPortfolios;
using CorePortfolio.API.Features.SavingGoals;
using CorePortfolio.API.Features.Analytics.GetAnalyticsInsights;
using CorePortfolio.API.Features.Rebalancing;
using CorePortfolio.Domain.Analytics;
using MediatR;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;

public sealed record AnalyticsScopeDto(
    Guid? PortfolioId,
    string PortfolioName,
    DateTime From,
    DateTime To,
    string Currency,
    bool FinancialHealthIsGlobal);

public sealed record AnalyticsGoalSummaryDto(
    int ActiveCount,
    int CompletedCount,
    int AtRiskCount,
    decimal TotalRemaining);

public sealed record AnalyticsDcaSummaryDto(
    int ActiveCount,
    int InsufficientCashCount,
    DateTime? NextExecutionDate);

public sealed record AnalyticsAttentionDto(
    string Code,
    string Severity,
    string Title,
    string Detail,
    string? DeepLink);

public sealed record AnalyticsOverviewDto(
    AnalyticsScopeDto Scope,
    PerformanceSummaryDto Performance,
    PerformanceSeriesDto Series,
    PerformanceDataQualityDto DataQuality,
    FinancialHealthDto FinancialHealth,
    IReadOnlyList<AssetAllocationDto> Allocation,
    IReadOnlyList<CashflowMonthlyAnalyticsDto> Cashflow,
    AnalyticsGoalSummaryDto Goals,
    AnalyticsDcaSummaryDto Dca,
    AnalyticsInsightsDto Insights,
    IReadOnlyList<AnalyticsAttentionDto> Attention);

public sealed record GetAnalyticsOverviewQuery(
    Guid? PortfolioId,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<AnalyticsOverviewDto>;

public sealed class GetAnalyticsOverviewHandler(
    IMediator mediator,
    IOptions<RebalancingOptions> rebalancingOptions)
    : IRequestHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewDto>
{
    private const int MaximumRangeDays = 3660;

    public async Task<AnalyticsOverviewDto> Handle(
        GetAnalyticsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD"))
            throw new RequestValidationException("Currency phải là VND hoặc USD.");

        var to = (request.To ?? DateTime.UtcNow).Date;
        var from = (request.From ?? to.AddMonths(-6)).Date;
        if (from > to)
            throw new RequestValidationException("Ngày bắt đầu không được sau ngày kết thúc.");
        if ((to - from).Days + 1 > MaximumRangeDays)
            throw new RequestValidationException("Khoảng phân tích không được vượt quá 10 năm.");

        var portfolios = await mediator.Send(new GetPortfoliosQuery(), cancellationToken);
        var selectedPortfolio = request.PortfolioId.HasValue
            ? portfolios.FirstOrDefault(portfolio => portfolio.Id == request.PortfolioId.Value)
            : null;
        if (request.PortfolioId.HasValue && selectedPortfolio is null)
            throw new ResourceNotFoundException("Không tìm thấy danh mục của người dùng.");

        var performance = await mediator.Send(
            new GetPerformanceSummaryQuery(
                request.PortfolioId,
                "All",
                from,
                to,
                currency),
            cancellationToken);
        var series = await mediator.Send(
            new GetPerformanceSeriesQuery(
                request.PortfolioId,
                "All",
                from,
                to,
                currency),
            cancellationToken);
        var dataQuality = await mediator.Send(
            new GetPerformanceDataQualityQuery(request.PortfolioId, from, to),
            cancellationToken);
        var financialHealth = await mediator.Send(
            new GetFinancialHealthQuery(currency),
            cancellationToken);
        var allocation = await mediator.Send(
            new GetAssetAllocationQuery(currency, request.PortfolioId),
            cancellationToken);
        var targetPlan = await mediator.Send(
            new GetTargetAllocationsQuery(),
            cancellationToken);

        var months = Math.Clamp(
            (int)Math.Ceiling(((to.Year - from.Year) * 12 + to.Month - from.Month + 1m)),
            3,
            12);
        var cashflow = await mediator.Send(
            new GetCashflowAnalyticsQuery(months, currency, request.PortfolioId),
            cancellationToken);
        var goals = await mediator.Send(new GetSavingGoalsQuery(), cancellationToken);
        var dcaPlans = await mediator.Send(new GetDcaPlansQuery(), cancellationToken);

        var scopedGoals = goals
            .Where(goal =>
                goal.Currency == currency &&
                (!request.PortfolioId.HasValue || goal.PortfolioId == request.PortfolioId.Value))
            .ToList();
        var scopedDcaPlans = dcaPlans
            .Where(plan =>
                plan.Currency == currency &&
                (!request.PortfolioId.HasValue || plan.PortfolioId == request.PortfolioId.Value))
            .ToList();
        var goalSummary = new AnalyticsGoalSummaryDto(
            scopedGoals.Count(goal => !goal.IsCompleted),
            scopedGoals.Count(goal => goal.IsCompleted),
            scopedGoals.Count(goal =>
                !goal.IsCompleted &&
                goal.DaysRemaining <= 30 &&
                goal.ProgressPercentage < 80m),
            scopedGoals.Where(goal => !goal.IsCompleted).Sum(goal => goal.RemainingAmount));
        var activeDcaPlans = scopedDcaPlans.Where(plan => plan.IsActive).ToList();
        var dcaSummary = new AnalyticsDcaSummaryDto(
            activeDcaPlans.Count,
            activeDcaPlans.Count(plan => !plan.HasEnoughCash),
            activeDcaPlans.Count == 0
                ? null
                : activeDcaPlans.Min(plan => plan.NextExecutionDate));

        var scope = new AnalyticsScopeDto(
            request.PortfolioId,
            selectedPortfolio?.Name ?? "Tất cả danh mục",
            from,
            to,
            currency,
            request.PortfolioId.HasValue);
        var findings = AnalyticsInsightEngine.Evaluate(new AnalyticsInsightInput(
            dataQuality.QualityStatus,
            dataQuality.MissingSnapshotDays,
            dataQuality.StaleAssetCount,
            dataQuality.UnclassifiedCashFlowCount,
            performance.TimeWeightedReturnPercentage.Value,
            performance.MoneyWeightedReturnPercentage.Value,
            performance.MaximumDrawdownPercentage.Value,
            targetPlan.Status,
            rebalancingOptions.Value.TolerancePercentagePoints,
            allocation.Select(item => new AnalyticsAllocationSignal(
                item.CategoryName,
                item.Percentage,
                item.TargetPercentage)).ToList(),
            cashflow.Select(item => item.NetFlow).ToList(),
            financialHealth.BudgetExceededCount,
            goalSummary.AtRiskCount,
            dcaSummary.InsufficientCashCount));
        var insights = AnalyticsInsightPresenter.Create(
            scope,
            findings,
            DateTime.UtcNow);
        var attention = insights.Items
            .Take(3)
            .Select(item => new AnalyticsAttentionDto(
                item.Code,
                item.Severity,
                item.Title,
                item.Observation,
                item.Action?.Href))
            .ToList();
        return new AnalyticsOverviewDto(
            scope,
            performance,
            series,
            dataQuality,
            financialHealth,
            allocation,
            cashflow,
            goalSummary,
            dcaSummary,
            insights,
            attention);
    }
}
