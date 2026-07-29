using CorePortfolio.Domain.Analytics;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class AnalyticsDecisionReviewEngineTests
{
    [Fact]
    public void Compare_ComputesMetricAndPortfolioValueChanges()
    {
        var result = AnalyticsDecisionReviewEngine.Compare(CreateInput(
            baselineValue: 100m,
            currentValue: 112m,
            baselineTwr: 5m,
            currentTwr: 8m));

        Assert.Equal(12m, result.TrackedPortfolioValue.Delta);
        Assert.Equal(12m, result.TrackedPortfolioValueChangePercentage);
        Assert.Equal(3m, result.TimeWeightedReturnPercentage.Delta);
    }

    [Fact]
    public void Compare_ZeroBaselineValue_DoesNotInventPercentage()
    {
        var result = AnalyticsDecisionReviewEngine.Compare(CreateInput(
            baselineValue: 0m,
            currentValue: 10m));

        Assert.Null(result.TrackedPortfolioValueChangePercentage);
    }

    [Fact]
    public void Compare_ClassifiesNewResolvedAndPersistentInsights()
    {
        var result = AnalyticsDecisionReviewEngine.Compare(CreateInput(
            baselineCodes: ["DATA_QUALITY", "DRAWDOWN"],
            currentCodes: ["DRAWDOWN", "CASHFLOW_PRESSURE"]));

        Assert.Equal(["CASHFLOW_PRESSURE"], result.NewInsightCodes);
        Assert.Equal(["DATA_QUALITY"], result.ResolvedInsightCodes);
        Assert.Equal(["DRAWDOWN"], result.PersistentInsightCodes);
    }

    [Theory]
    [InlineData(true, "Complete", "Ready", "High")]
    [InlineData(true, "StalePrices", "Caution", "Medium")]
    [InlineData(true, "Partial", "Caution", "Medium")]
    [InlineData(true, "Unavailable", "Unavailable", "Low")]
    [InlineData(false, "Complete", "Unavailable", "Low")]
    public void Compare_ReadinessFollowsScopeAndCurrentDataQuality(
        bool scopeAvailable,
        string qualityStatus,
        string expectedReadiness,
        string expectedConfidence)
    {
        var result = AnalyticsDecisionReviewEngine.Compare(CreateInput(
            scopeAvailable: scopeAvailable,
            currentQualityStatus: qualityStatus));

        Assert.Equal(expectedReadiness, result.Readiness);
        Assert.Equal(expectedConfidence, result.Confidence);
    }

    [Fact]
    public void Compare_MissingCurrentMetric_PreservesUnavailableDelta()
    {
        var result = AnalyticsDecisionReviewEngine.Compare(CreateInput(
            baselineTwr: 4m,
            currentTwr: null));

        Assert.Null(result.TimeWeightedReturnPercentage.Delta);
    }

    [Fact]
    public void Compare_UnavailableComparison_DoesNotClaimInsightsResolved()
    {
        var input = CreateInput(
            scopeAvailable: false,
            baselineCodes: ["DRAWDOWN"],
            currentCodes: []) with
        {
            BaselineQualityStatus = "Complete"
        };

        var result = AnalyticsDecisionReviewEngine.Compare(input);

        Assert.Empty(result.ResolvedInsightCodes);
        Assert.Null(result.TrackedPortfolioValue.Current);
        Assert.Null(result.TrackedPortfolioValue.Delta);
    }

    [Fact]
    public void Compare_PartialBaseline_LimitsCurrentComparisonConfidence()
    {
        var input = CreateInput(currentQualityStatus: "Complete") with
        {
            BaselineQualityStatus = "Partial"
        };

        var result = AnalyticsDecisionReviewEngine.Compare(input);

        Assert.Equal(AnalyticsDecisionReviewReadiness.Caution, result.Readiness);
        Assert.Equal(AnalyticsInsightConfidences.Medium, result.Confidence);
    }

    private static AnalyticsDecisionReviewInput CreateInput(
        bool scopeAvailable = true,
        string currentQualityStatus = "Complete",
        decimal baselineValue = 100m,
        decimal? currentValue = 100m,
        decimal? baselineTwr = 5m,
        decimal? currentTwr = 5m,
        IReadOnlyList<string>? baselineCodes = null,
        IReadOnlyList<string>? currentCodes = null) =>
        new(
            scopeAvailable,
            "Complete",
            currentQualityStatus,
            baselineValue,
            currentValue,
            baselineTwr,
            currentTwr,
            4m,
            4m,
            -3m,
            -3m,
            baselineCodes ?? [],
            currentCodes ?? []);
}
