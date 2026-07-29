using CorePortfolio.Domain.Analytics;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class AnalyticsInsightEngineTests
{
    [Fact]
    public void Evaluate_UnavailableData_IsHighestPriority()
    {
        var result = AnalyticsInsightEngine.Evaluate(CreateInput(
            qualityStatus: "Unavailable",
            budgetExceededCount: 1));

        Assert.Equal("DATA_UNAVAILABLE", result[0].Code);
        Assert.Equal(AnalyticsInsightSeverities.Critical, result[0].Severity);
        Assert.Equal(AnalyticsInsightConfidences.High, result[0].Confidence);
    }

    [Theory]
    [InlineData(-8, "Warning")]
    [InlineData(-15, "Critical")]
    public void Evaluate_Drawdown_UsesDocumentedThresholds(
        decimal drawdown,
        string expectedSeverity)
    {
        var result = AnalyticsInsightEngine.Evaluate(CreateInput(
            maximumDrawdownPercentage: drawdown));

        var finding = Assert.Single(result, item => item.Code == "DRAWDOWN");
        Assert.Equal(expectedSeverity, finding.Severity);
    }

    [Fact]
    public void Evaluate_AllocationDrift_RequiresCompleteTargetPlan()
    {
        var allocation = new[]
        {
            new AnalyticsAllocationSignal("Stock", 70m, 50m),
            new AnalyticsAllocationSignal("Fund", 30m, 50m)
        };

        var incomplete = AnalyticsInsightEngine.Evaluate(CreateInput(
            targetPlanStatus: "Invalid",
            allocation: allocation));
        var complete = AnalyticsInsightEngine.Evaluate(CreateInput(
            targetPlanStatus: "Complete",
            allocation: allocation));

        Assert.DoesNotContain(incomplete, item => item.Code == "ALLOCATION_DRIFT");
        Assert.Contains(complete, item => item.Code == "ALLOCATION_DRIFT");
    }

    [Fact]
    public void Evaluate_CashflowPressure_RequiresTwoNegativeRecentMonths()
    {
        var result = AnalyticsInsightEngine.Evaluate(CreateInput(
            recentMonthlyNetFlows: [10m, -5m, -8m]));

        var finding = Assert.Single(result, item => item.Code == "CASHFLOW_PRESSURE");
        Assert.Contains(
            finding.Evidence,
            evidence => evidence.Key == "negativeMonthCount" && evidence.Value == 2m);
    }

    [Fact]
    public void Evaluate_ReturnGap_ConfidenceFollowsDataQuality()
    {
        var result = AnalyticsInsightEngine.Evaluate(CreateInput(
            qualityStatus: "Partial",
            timeWeightedReturnPercentage: 12m,
            moneyWeightedReturnPercentage: 4m));

        var finding = Assert.Single(result, item => item.Code == "RETURN_GAP");
        Assert.Equal(AnalyticsInsightConfidences.Low, finding.Confidence);
    }

    [Fact]
    public void Evaluate_NoTriggeredRule_ReturnsPositiveFinding()
    {
        var result = AnalyticsInsightEngine.Evaluate(CreateInput());

        var finding = Assert.Single(result);
        Assert.Equal("NO_URGENT_SIGNAL", finding.Code);
        Assert.Equal(AnalyticsInsightSeverities.Positive, finding.Severity);
    }

    [Theory]
    [InlineData(1, "Info")]
    [InlineData(3, "Warning")]
    public void Evaluate_DecisionReviewsDue_AreSurfaced(
        int dueCount,
        string expectedSeverity)
    {
        var input = CreateInput() with { DecisionReviewDueCount = dueCount };

        var finding = Assert.Single(
            AnalyticsInsightEngine.Evaluate(input),
            item => item.Code == "DECISION_REVIEW_DUE");
        Assert.Equal(expectedSeverity, finding.Severity);
    }

    private static AnalyticsInsightInput CreateInput(
        string qualityStatus = "Complete",
        decimal? timeWeightedReturnPercentage = 4m,
        decimal? moneyWeightedReturnPercentage = 4m,
        decimal? maximumDrawdownPercentage = -2m,
        string targetPlanStatus = "NotConfigured",
        IReadOnlyList<AnalyticsAllocationSignal>? allocation = null,
        IReadOnlyList<decimal>? recentMonthlyNetFlows = null,
        int budgetExceededCount = 0) =>
        new(
            qualityStatus,
            0,
            0,
            0,
            timeWeightedReturnPercentage,
            moneyWeightedReturnPercentage,
            maximumDrawdownPercentage,
            targetPlanStatus,
            5m,
            allocation ?? [],
            recentMonthlyNetFlows ?? [10m, 8m, 12m],
            budgetExceededCount,
            0,
            0,
            0);
}
