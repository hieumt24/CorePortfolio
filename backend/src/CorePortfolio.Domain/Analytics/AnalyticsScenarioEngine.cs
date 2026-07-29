namespace CorePortfolio.Domain.Analytics;

public sealed record AnalyticsScenarioAllocationInput(
    string CategoryName,
    decimal CurrentValue,
    decimal ShockPercentage);

public sealed record AnalyticsScenarioInput(
    string QualityStatus,
    int HorizonMonths,
    decimal MonthlyIncomeChange,
    decimal MonthlyExpenseChange,
    IReadOnlyList<decimal> HistoricalMonthlyNetFlows,
    IReadOnlyList<AnalyticsScenarioAllocationInput> Allocations);

public sealed record AnalyticsScenarioAllocationOutcome(
    string CategoryName,
    decimal CurrentValue,
    decimal ShockPercentage,
    decimal StressedValue,
    decimal ValueChange,
    decimal CurrentPercentage,
    decimal StressedPercentage);

public sealed record AnalyticsScenarioOutcome(
    string Confidence,
    int HorizonMonths,
    int CashflowSampleMonthCount,
    decimal BaselinePortfolioValue,
    decimal StressedPortfolioValue,
    decimal PortfolioValueChange,
    decimal PortfolioValueChangePercentage,
    decimal BaselineMonthlyNetFlow,
    decimal ScenarioMonthlyNetFlow,
    decimal BaselineCumulativeNetFlow,
    decimal ScenarioCumulativeNetFlow,
    decimal CumulativeNetFlowDifference,
    decimal CombinedPlanningDelta,
    decimal BreakEvenMonthlyImprovement,
    string? WorstAffectedCategory,
    IReadOnlyList<AnalyticsScenarioAllocationOutcome> Allocations);

public static class AnalyticsScenarioEngine
{
    public static AnalyticsScenarioOutcome Evaluate(AnalyticsScenarioInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.HorizonMonths is < 1 or > 60)
            throw new ArgumentOutOfRangeException(
                nameof(input.HorizonMonths),
                "Scenario horizon must be between 1 and 60 months.");
        ArgumentNullException.ThrowIfNull(input.HistoricalMonthlyNetFlows);
        ArgumentNullException.ThrowIfNull(input.Allocations);

        foreach (var allocation in input.Allocations)
        {
            if (string.IsNullOrWhiteSpace(allocation.CategoryName))
                throw new ArgumentException(
                    "Scenario category is required.",
                    nameof(input.Allocations));
            if (allocation.CurrentValue < 0m)
                throw new ArgumentOutOfRangeException(
                    nameof(input.Allocations),
                    "Current allocation value cannot be negative.");
            if (allocation.ShockPercentage is < -100m or > 300m)
                throw new ArgumentOutOfRangeException(
                    nameof(input.Allocations),
                    "Scenario shock must be between -100% and 300%.");
        }

        var duplicateCategory = input.Allocations
            .GroupBy(item => item.CategoryName.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCategory is not null)
            throw new ArgumentException(
                $"Duplicate scenario category: {duplicateCategory.Key}.",
                nameof(input.Allocations));

        var baselinePortfolioValue = input.Allocations.Sum(item => item.CurrentValue);
        var provisionalAllocations = input.Allocations
            .Select(item =>
            {
                var stressedValue = item.CurrentValue * (1m + item.ShockPercentage / 100m);
                return new
                {
                    item.CategoryName,
                    item.CurrentValue,
                    item.ShockPercentage,
                    StressedValue = stressedValue,
                    ValueChange = stressedValue - item.CurrentValue
                };
            })
            .ToList();
        var stressedPortfolioValue = provisionalAllocations.Sum(item => item.StressedValue);
        var allocationOutcomes = provisionalAllocations
            .Select(item => new AnalyticsScenarioAllocationOutcome(
                item.CategoryName,
                item.CurrentValue,
                item.ShockPercentage,
                item.StressedValue,
                item.ValueChange,
                baselinePortfolioValue == 0m
                    ? 0m
                    : item.CurrentValue / baselinePortfolioValue * 100m,
                stressedPortfolioValue == 0m
                    ? 0m
                    : item.StressedValue / stressedPortfolioValue * 100m))
            .OrderByDescending(item => item.StressedValue)
            .ThenBy(item => item.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var portfolioValueChange = stressedPortfolioValue - baselinePortfolioValue;
        var baselineMonthlyNetFlow = input.HistoricalMonthlyNetFlows.Count == 0
            ? 0m
            : input.HistoricalMonthlyNetFlows.Average();
        var scenarioMonthlyNetFlow = baselineMonthlyNetFlow +
            input.MonthlyIncomeChange -
            input.MonthlyExpenseChange;
        var baselineCumulativeNetFlow = baselineMonthlyNetFlow * input.HorizonMonths;
        var scenarioCumulativeNetFlow = scenarioMonthlyNetFlow * input.HorizonMonths;
        var cumulativeNetFlowDifference =
            scenarioCumulativeNetFlow - baselineCumulativeNetFlow;

        return new AnalyticsScenarioOutcome(
            ResolveConfidence(input.QualityStatus),
            input.HorizonMonths,
            input.HistoricalMonthlyNetFlows.Count,
            baselinePortfolioValue,
            stressedPortfolioValue,
            portfolioValueChange,
            baselinePortfolioValue == 0m
                ? 0m
                : portfolioValueChange / baselinePortfolioValue * 100m,
            baselineMonthlyNetFlow,
            scenarioMonthlyNetFlow,
            baselineCumulativeNetFlow,
            scenarioCumulativeNetFlow,
            cumulativeNetFlowDifference,
            portfolioValueChange + cumulativeNetFlowDifference,
            Math.Max(0m, -scenarioMonthlyNetFlow),
            allocationOutcomes
                .Where(item => item.ValueChange < 0m)
                .OrderBy(item => item.ValueChange)
                .Select(item => item.CategoryName)
                .FirstOrDefault(),
            allocationOutcomes);
    }

    private static string ResolveConfidence(string qualityStatus) =>
        qualityStatus switch
        {
            "Complete" => AnalyticsInsightConfidences.High,
            "StalePrices" => AnalyticsInsightConfidences.Medium,
            _ => AnalyticsInsightConfidences.Low
        };
}
