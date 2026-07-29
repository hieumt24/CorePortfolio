using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Dashboard.GetFinancialHealth;
using CorePortfolio.API.Features.DcaPlans;
using CorePortfolio.API.Features.Performance;
using CorePortfolio.API.Features.Performance.GetPerformanceDataQuality;
using CorePortfolio.API.Features.Performance.GetPerformanceSeries;
using CorePortfolio.API.Features.Performance.GetPerformanceSummary;
using CorePortfolio.API.Features.Portfolios.GetPortfolios;
using CorePortfolio.API.Features.SavingGoals;
using MediatR;

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
    IReadOnlyList<AnalyticsAttentionDto> Attention);

public sealed record GetAnalyticsOverviewQuery(
    Guid? PortfolioId,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<AnalyticsOverviewDto>;

public sealed class GetAnalyticsOverviewHandler(IMediator mediator)
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

        var attention = BuildAttention(dataQuality, performance, goalSummary, dcaSummary);
        return new AnalyticsOverviewDto(
            new AnalyticsScopeDto(
                request.PortfolioId,
                selectedPortfolio?.Name ?? "Tất cả danh mục",
                from,
                to,
                currency,
                request.PortfolioId.HasValue),
            performance,
            series,
            dataQuality,
            financialHealth,
            allocation,
            cashflow,
            goalSummary,
            dcaSummary,
            attention);
    }

    private static IReadOnlyList<AnalyticsAttentionDto> BuildAttention(
        PerformanceDataQualityDto quality,
        PerformanceSummaryDto performance,
        AnalyticsGoalSummaryDto goals,
        AnalyticsDcaSummaryDto dca)
    {
        var result = new List<AnalyticsAttentionDto>();
        if (quality.QualityStatus != "Complete")
        {
            result.Add(new AnalyticsAttentionDto(
                "DATA_QUALITY",
                quality.QualityStatus == "Unavailable" ? "Critical" : "Warning",
                "Kiểm tra độ tin cậy dữ liệu",
                $"{quality.MissingSnapshotDays} ngày thiếu snapshot, {quality.StaleAssetCount} tài sản có giá cũ.",
                "/analytics/performance"));
        }

        if (performance.MaximumDrawdownPercentage.Value is < -10m)
        {
            result.Add(new AnalyticsAttentionDto(
                "DRAWDOWN",
                "Warning",
                "Rà soát mức sụt giảm",
                $"Mức drawdown lớn nhất trong kỳ là {performance.MaximumDrawdownPercentage.Value:0.##}%.",
                "/analytics/performance"));
        }

        if (goals.AtRiskCount > 0)
        {
            result.Add(new AnalyticsAttentionDto(
                "GOALS_AT_RISK",
                "Warning",
                "Mục tiêu sắp đến hạn",
                $"{goals.AtRiskCount} mục tiêu còn dưới 80% tiến độ và còn không quá 30 ngày.",
                "/saving-goals"));
        }

        if (dca.InsufficientCashCount > 0)
        {
            result.Add(new AnalyticsAttentionDto(
                "DCA_CASH",
                "Info",
                "Kiểm tra tiền cho kế hoạch DCA",
                $"{dca.InsufficientCashCount} kế hoạch đang hoạt động chưa đủ số dư tiền mặt.",
                "/dca-plans"));
        }

        if (result.Count == 0)
        {
            result.Add(new AnalyticsAttentionDto(
                "NO_URGENT_SIGNAL",
                "Positive",
                "Chưa có tín hiệu cần xử lý ngay",
                "Dữ liệu hiện tại không phát hiện vấn đề nổi bật theo các quy tắc Sprint 1.",
                null));
        }

        return result.Take(3).ToList();
    }
}
