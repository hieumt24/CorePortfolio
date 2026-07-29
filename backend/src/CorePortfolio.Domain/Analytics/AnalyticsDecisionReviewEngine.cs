namespace CorePortfolio.Domain.Analytics;

public static class AnalyticsDecisionReviewReadiness
{
    public const string Ready = "Ready";
    public const string Caution = "Caution";
    public const string Unavailable = "Unavailable";
}

public sealed record AnalyticsDecisionReviewInput(
    bool IsScopeAvailable,
    string BaselineQualityStatus,
    string? CurrentQualityStatus,
    decimal BaselineTrackedPortfolioValue,
    decimal? CurrentTrackedPortfolioValue,
    decimal? BaselineTimeWeightedReturnPercentage,
    decimal? CurrentTimeWeightedReturnPercentage,
    decimal? BaselineMoneyWeightedReturnPercentage,
    decimal? CurrentMoneyWeightedReturnPercentage,
    decimal? BaselineMaximumDrawdownPercentage,
    decimal? CurrentMaximumDrawdownPercentage,
    IReadOnlyList<string> BaselineInsightCodes,
    IReadOnlyList<string> CurrentInsightCodes);

public sealed record AnalyticsDecisionMetricComparison(
    decimal? Baseline,
    decimal? Current,
    decimal? Delta);

public sealed record AnalyticsDecisionReviewComparison(
    string Readiness,
    string Confidence,
    AnalyticsDecisionMetricComparison TrackedPortfolioValue,
    decimal? TrackedPortfolioValueChangePercentage,
    AnalyticsDecisionMetricComparison TimeWeightedReturnPercentage,
    AnalyticsDecisionMetricComparison MoneyWeightedReturnPercentage,
    AnalyticsDecisionMetricComparison MaximumDrawdownPercentage,
    IReadOnlyList<string> NewInsightCodes,
    IReadOnlyList<string> ResolvedInsightCodes,
    IReadOnlyList<string> PersistentInsightCodes);

public static class AnalyticsDecisionReviewEngine
{
    public static AnalyticsDecisionReviewComparison Compare(
        AnalyticsDecisionReviewInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.BaselineInsightCodes);
        ArgumentNullException.ThrowIfNull(input.CurrentInsightCodes);

        var readiness = ResolveReadiness(
            input.IsScopeAvailable,
            input.BaselineQualityStatus,
            input.CurrentQualityStatus);
        var confidence = readiness switch
        {
            AnalyticsDecisionReviewReadiness.Ready =>
                AnalyticsInsightConfidences.High,
            AnalyticsDecisionReviewReadiness.Caution =>
                AnalyticsInsightConfidences.Medium,
            _ => AnalyticsInsightConfidences.Low
        };
        var baselineCodes = NormalizeCodes(input.BaselineInsightCodes);
        var currentCodes = NormalizeCodes(input.CurrentInsightCodes);
        var hasComparableCurrent =
            readiness != AnalyticsDecisionReviewReadiness.Unavailable;

        return new AnalyticsDecisionReviewComparison(
            readiness,
            confidence,
            CompareMetric(
                input.BaselineTrackedPortfolioValue,
                hasComparableCurrent
                    ? input.CurrentTrackedPortfolioValue
                    : null),
            hasComparableCurrent &&
                input.CurrentTrackedPortfolioValue.HasValue &&
                input.BaselineTrackedPortfolioValue != 0m
                    ? (input.CurrentTrackedPortfolioValue.Value -
                        input.BaselineTrackedPortfolioValue) /
                        input.BaselineTrackedPortfolioValue * 100m
                    : null,
            CompareMetric(
                input.BaselineTimeWeightedReturnPercentage,
                hasComparableCurrent
                    ? input.CurrentTimeWeightedReturnPercentage
                    : null),
            CompareMetric(
                input.BaselineMoneyWeightedReturnPercentage,
                hasComparableCurrent
                    ? input.CurrentMoneyWeightedReturnPercentage
                    : null),
            CompareMetric(
                input.BaselineMaximumDrawdownPercentage,
                hasComparableCurrent
                    ? input.CurrentMaximumDrawdownPercentage
                    : null),
            (hasComparableCurrent
                ? currentCodes.Except(baselineCodes, StringComparer.Ordinal)
                : [])
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList(),
            (hasComparableCurrent
                ? baselineCodes.Except(currentCodes, StringComparer.Ordinal)
                : [])
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList(),
            (hasComparableCurrent
                ? baselineCodes.Intersect(currentCodes, StringComparer.Ordinal)
                : [])
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList());
    }

    private static string ResolveReadiness(
        bool isScopeAvailable,
        string baselineQualityStatus,
        string? currentQualityStatus)
    {
        if (!isScopeAvailable ||
            baselineQualityStatus == "Unavailable" ||
            string.IsNullOrWhiteSpace(currentQualityStatus) ||
            currentQualityStatus == "Unavailable")
        {
            return AnalyticsDecisionReviewReadiness.Unavailable;
        }

        return baselineQualityStatus == "Complete" &&
            currentQualityStatus == "Complete"
            ? AnalyticsDecisionReviewReadiness.Ready
            : AnalyticsDecisionReviewReadiness.Caution;
    }

    private static AnalyticsDecisionMetricComparison CompareMetric(
        decimal? baseline,
        decimal? current) =>
        new(
            baseline,
            current,
            baseline.HasValue && current.HasValue
                ? current.Value - baseline.Value
                : null);

    private static IReadOnlyList<string> NormalizeCodes(
        IEnumerable<string> codes) =>
        codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
}
