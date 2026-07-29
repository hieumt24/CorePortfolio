using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using CorePortfolio.Domain.Analytics;
using MediatR;

namespace CorePortfolio.API.Features.Analytics.EvaluateAnalyticsScenario;

public sealed record AnalyticsScenarioShockRequest(
    string CategoryName,
    decimal ChangePercentage);

public sealed record EvaluateAnalyticsScenarioRequest(
    Guid? PortfolioId,
    DateTime? From,
    DateTime? To,
    string Currency,
    int HorizonMonths,
    decimal MonthlyIncomeChange,
    decimal MonthlyExpenseChange,
    IReadOnlyList<AnalyticsScenarioShockRequest>? Shocks);

public sealed record AnalyticsScenarioAllocationDto(
    string CategoryName,
    decimal CurrentValue,
    decimal ShockPercentage,
    decimal StressedValue,
    decimal ValueChange,
    decimal CurrentPercentage,
    decimal StressedPercentage);

public sealed record AnalyticsScenarioBaselineDto(
    decimal TrackedPortfolioValue,
    decimal AverageMonthlyNetFlow,
    int CashflowSampleMonthCount);

public sealed record AnalyticsScenarioOutcomeDto(
    decimal StressedPortfolioValue,
    decimal PortfolioValueChange,
    decimal PortfolioValueChangePercentage,
    decimal ScenarioMonthlyNetFlow,
    decimal BaselineCumulativeNetFlow,
    decimal ScenarioCumulativeNetFlow,
    decimal CumulativeNetFlowDifference,
    decimal CombinedPlanningDelta,
    decimal BreakEvenMonthlyImprovement,
    string? WorstAffectedCategory);

public sealed record AnalyticsScenarioDto(
    AnalyticsScopeDto Scope,
    DateTime GeneratedAt,
    string MethodologyVersion,
    string Confidence,
    int HorizonMonths,
    AnalyticsScenarioBaselineDto Baseline,
    AnalyticsScenarioOutcomeDto Outcome,
    IReadOnlyList<AnalyticsScenarioAllocationDto> Allocations,
    IReadOnlyList<string> Assumptions,
    string Disclaimer);

public sealed record EvaluateAnalyticsScenarioQuery(
    EvaluateAnalyticsScenarioRequest Request) : IRequest<AnalyticsScenarioDto>;

public sealed class EvaluateAnalyticsScenarioHandler(IMediator mediator)
    : IRequestHandler<EvaluateAnalyticsScenarioQuery, AnalyticsScenarioDto>
{
    private const decimal MaximumMonthlyChange = 1_000_000_000_000_000m;

    public async Task<AnalyticsScenarioDto> Handle(
        EvaluateAnalyticsScenarioQuery query,
        CancellationToken cancellationToken)
    {
        var request = query.Request;
        Validate(request);

        var overview = await mediator.Send(
            new GetAnalyticsOverviewQuery(
                request.PortfolioId,
                request.From,
                request.To,
                request.Currency),
            cancellationToken);
        var shocks = (request.Shocks ?? [])
            .ToDictionary(
                shock => shock.CategoryName.Trim(),
                shock => shock.ChangePercentage,
                StringComparer.OrdinalIgnoreCase);
        var knownCategories = overview.Allocation
            .Select(item => item.CategoryName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownCategory = shocks.Keys
            .FirstOrDefault(category => !knownCategories.Contains(category));
        if (unknownCategory is not null)
            throw new RequestValidationException(
                $"Nhóm tài sản '{unknownCategory}' không có trong phạm vi phân tích.");

        var outcome = AnalyticsScenarioEngine.Evaluate(new AnalyticsScenarioInput(
            overview.DataQuality.QualityStatus,
            request.HorizonMonths,
            request.MonthlyIncomeChange,
            request.MonthlyExpenseChange,
            overview.Cashflow.Select(item => item.NetFlow).ToList(),
            overview.Allocation.Select(item => new AnalyticsScenarioAllocationInput(
                item.CategoryName,
                item.TotalValue,
                shocks.GetValueOrDefault(item.CategoryName))).ToList()));

        return new AnalyticsScenarioDto(
            overview.Scope,
            DateTime.UtcNow,
            "scenario-rules-v1",
            outcome.Confidence,
            outcome.HorizonMonths,
            new AnalyticsScenarioBaselineDto(
                outcome.BaselinePortfolioValue,
                outcome.BaselineMonthlyNetFlow,
                outcome.CashflowSampleMonthCount),
            new AnalyticsScenarioOutcomeDto(
                outcome.StressedPortfolioValue,
                outcome.PortfolioValueChange,
                outcome.PortfolioValueChangePercentage,
                outcome.ScenarioMonthlyNetFlow,
                outcome.BaselineCumulativeNetFlow,
                outcome.ScenarioCumulativeNetFlow,
                outcome.CumulativeNetFlowDifference,
                outcome.CombinedPlanningDelta,
                outcome.BreakEvenMonthlyImprovement,
                outcome.WorstAffectedCategory),
            outcome.Allocations.Select(item => new AnalyticsScenarioAllocationDto(
                item.CategoryName,
                item.CurrentValue,
                item.ShockPercentage,
                item.StressedValue,
                item.ValueChange,
                item.CurrentPercentage,
                item.StressedPercentage)).ToList(),
            [
                "Cú sốc giá được áp dụng một lần trên giá trị danh mục đang theo dõi.",
                "Dòng tiền nền là trung bình số học của các tháng trong phạm vi đã chọn.",
                "Thay đổi thu và chi được giữ cố định trong toàn bộ kỳ mô phỏng.",
                "Không giả định lợi suất tương lai, lạm phát, thuế, phí, lãi suất hoặc trượt giá."
            ],
            "Mô phỏng chỉ giúp kiểm tra giả định và không phải dự báo hay khuyến nghị đầu tư.");
    }

    private static void Validate(EvaluateAnalyticsScenarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new RequestValidationException(
                "Đơn vị tiền tệ là bắt buộc.");
        if (request.HorizonMonths is < 1 or > 60)
            throw new RequestValidationException(
                "Thời hạn mô phỏng phải từ 1 đến 60 tháng.");
        if (Math.Abs(request.MonthlyIncomeChange) > MaximumMonthlyChange ||
            Math.Abs(request.MonthlyExpenseChange) > MaximumMonthlyChange)
        {
            throw new RequestValidationException(
                "Thay đổi thu hoặc chi theo tháng vượt quá giới hạn hỗ trợ.");
        }

        var shocks = request.Shocks ?? [];
        var duplicate = shocks
            .GroupBy(
                shock => shock.CategoryName?.Trim() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new RequestValidationException(
                $"Nhóm tài sản '{duplicate.Key}' xuất hiện nhiều lần.");

        foreach (var shock in shocks)
        {
            if (string.IsNullOrWhiteSpace(shock.CategoryName))
                throw new RequestValidationException(
                    "Tên nhóm tài sản là bắt buộc.");
            if (shock.ChangePercentage is < -100m or > 300m)
                throw new RequestValidationException(
                    "Cú sốc giá phải nằm trong khoảng -100% đến 300%.");
        }
    }
}
