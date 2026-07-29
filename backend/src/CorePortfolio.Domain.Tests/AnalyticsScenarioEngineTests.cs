using CorePortfolio.Domain.Analytics;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class AnalyticsScenarioEngineTests
{
    [Fact]
    public void Evaluate_AppliesIndependentCategoryShocks()
    {
        var result = AnalyticsScenarioEngine.Evaluate(CreateInput(
            allocations:
            [
                new("Stock", 600m, -10m),
                new("Crypto", 400m, -25m)
            ]));

        Assert.Equal(840m, result.StressedPortfolioValue);
        Assert.Equal(-160m, result.PortfolioValueChange);
        Assert.Equal(-16m, result.PortfolioValueChangePercentage);
        Assert.Equal("Crypto", result.WorstAffectedCategory);
        Assert.Equal(2, result.Allocations.Count);
    }

    [Fact]
    public void Evaluate_SeparatesCashflowChangeFromPortfolioShock()
    {
        var result = AnalyticsScenarioEngine.Evaluate(CreateInput(
            horizonMonths: 6,
            monthlyIncomeChange: 10m,
            monthlyExpenseChange: 4m,
            historicalMonthlyNetFlows: [2m, 4m, 6m],
            allocations: [new("Stock", 100m, -10m)]));

        Assert.Equal(4m, result.BaselineMonthlyNetFlow);
        Assert.Equal(10m, result.ScenarioMonthlyNetFlow);
        Assert.Equal(24m, result.BaselineCumulativeNetFlow);
        Assert.Equal(60m, result.ScenarioCumulativeNetFlow);
        Assert.Equal(36m, result.CumulativeNetFlowDifference);
        Assert.Equal(26m, result.CombinedPlanningDelta);
    }

    [Fact]
    public void Evaluate_NegativeScenarioFlow_ReportsBreakEvenImprovement()
    {
        var result = AnalyticsScenarioEngine.Evaluate(CreateInput(
            monthlyExpenseChange: 8m,
            historicalMonthlyNetFlows: [5m]));

        Assert.Equal(-3m, result.ScenarioMonthlyNetFlow);
        Assert.Equal(3m, result.BreakEvenMonthlyImprovement);
    }

    [Theory]
    [InlineData("Complete", "High")]
    [InlineData("StalePrices", "Medium")]
    [InlineData("Partial", "Low")]
    [InlineData("Unavailable", "Low")]
    public void Evaluate_ConfidenceFollowsDataQuality(
        string qualityStatus,
        string expectedConfidence)
    {
        var result = AnalyticsScenarioEngine.Evaluate(CreateInput(
            qualityStatus: qualityStatus));

        Assert.Equal(expectedConfidence, result.Confidence);
    }

    [Fact]
    public void Evaluate_RejectsImpossibleLoss()
    {
        var input = CreateInput(
            allocations: [new("Stock", 100m, -101m)]);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => AnalyticsScenarioEngine.Evaluate(input));
    }

    [Fact]
    public void Evaluate_RejectsDuplicateCategoriesIgnoringCase()
    {
        var input = CreateInput(
            allocations:
            [
                new("Stock", 60m, -10m),
                new("stock", 40m, -5m)
            ]);

        Assert.Throws<ArgumentException>(
            () => AnalyticsScenarioEngine.Evaluate(input));
    }

    private static AnalyticsScenarioInput CreateInput(
        string qualityStatus = "Complete",
        int horizonMonths = 12,
        decimal monthlyIncomeChange = 0m,
        decimal monthlyExpenseChange = 0m,
        IReadOnlyList<decimal>? historicalMonthlyNetFlows = null,
        IReadOnlyList<AnalyticsScenarioAllocationInput>? allocations = null) =>
        new(
            qualityStatus,
            horizonMonths,
            monthlyIncomeChange,
            monthlyExpenseChange,
            historicalMonthlyNetFlows ?? [5m, 5m, 5m],
            allocations ?? [new("Stock", 100m, 0m)]);
}
