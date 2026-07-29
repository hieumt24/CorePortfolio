namespace CorePortfolio.Domain.Analytics;

public static class AnalyticsInsightSeverities
{
    public const string Critical = "Critical";
    public const string Warning = "Warning";
    public const string Info = "Info";
    public const string Positive = "Positive";
}

public static class AnalyticsInsightConfidences
{
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
}

public static class AnalyticsInsightCategories
{
    public const string DataQuality = "DataQuality";
    public const string Risk = "Risk";
    public const string Allocation = "Allocation";
    public const string Cashflow = "Cashflow";
    public const string Goals = "Goals";
    public const string Performance = "Performance";
    public const string General = "General";
}

public sealed record AnalyticsAllocationSignal(
    string CategoryName,
    decimal Percentage,
    decimal TargetPercentage);

public sealed record AnalyticsInsightInput(
    string QualityStatus,
    int MissingSnapshotDays,
    int StaleAssetCount,
    int UnclassifiedCashFlowCount,
    decimal? TimeWeightedReturnPercentage,
    decimal? MoneyWeightedReturnPercentage,
    decimal? MaximumDrawdownPercentage,
    string TargetPlanStatus,
    decimal AllocationTolerancePercentagePoints,
    IReadOnlyList<AnalyticsAllocationSignal> Allocation,
    IReadOnlyList<decimal> RecentMonthlyNetFlows,
    int BudgetExceededCount,
    int GoalAtRiskCount,
    int DcaInsufficientCashCount);

public sealed record AnalyticsInsightEvidence(
    string Key,
    decimal Value,
    string Unit);

public sealed record AnalyticsInsightFinding(
    string Code,
    string Category,
    string Severity,
    string Confidence,
    int Priority,
    string? Subject,
    IReadOnlyList<AnalyticsInsightEvidence> Evidence);

public static class AnalyticsInsightEngine
{
    private const string CompleteQuality = "Complete";
    private const string UnavailableQuality = "Unavailable";
    private const string CompleteTargetPlan = "Complete";

    public static IReadOnlyList<AnalyticsInsightFinding> Evaluate(
        AnalyticsInsightInput input)
    {
        var findings = new List<AnalyticsInsightFinding>();
        AddDataQualityFinding(input, findings);
        AddBudgetFinding(input, findings);
        AddDrawdownFinding(input, findings);
        AddAllocationFinding(input, findings);
        AddCashflowFinding(input, findings);
        AddGoalFinding(input, findings);
        AddDcaFinding(input, findings);
        AddReturnGapFinding(input, findings);

        if (findings.Count == 0)
        {
            findings.Add(new AnalyticsInsightFinding(
                "NO_URGENT_SIGNAL",
                AnalyticsInsightCategories.General,
                AnalyticsInsightSeverities.Positive,
                AnalyticsInsightConfidences.Medium,
                10,
                null,
                []));
        }

        return findings
            .OrderByDescending(finding => finding.Priority)
            .ThenBy(finding => finding.Code, StringComparer.Ordinal)
            .Take(8)
            .ToList();
    }

    private static void AddDataQualityFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (input.QualityStatus == CompleteQuality)
            return;

        findings.Add(new AnalyticsInsightFinding(
            input.QualityStatus == UnavailableQuality
                ? "DATA_UNAVAILABLE"
                : "DATA_QUALITY",
            AnalyticsInsightCategories.DataQuality,
            input.QualityStatus == UnavailableQuality
                ? AnalyticsInsightSeverities.Critical
                : AnalyticsInsightSeverities.Warning,
            AnalyticsInsightConfidences.High,
            input.QualityStatus == UnavailableQuality ? 100 : 92,
            null,
            [
                new("missingSnapshotDays", input.MissingSnapshotDays, "days"),
                new("staleAssetCount", input.StaleAssetCount, "assets"),
                new("unclassifiedCashFlowCount", input.UnclassifiedCashFlowCount, "records")
            ]));
    }

    private static void AddBudgetFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (input.BudgetExceededCount <= 0)
            return;

        findings.Add(new AnalyticsInsightFinding(
            "BUDGET_EXCEEDED",
            AnalyticsInsightCategories.Cashflow,
            AnalyticsInsightSeverities.Critical,
            AnalyticsInsightConfidences.High,
            96,
            null,
            [new("budgetExceededCount", input.BudgetExceededCount, "budgets")]));
    }

    private static void AddDrawdownFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (!input.MaximumDrawdownPercentage.HasValue)
            return;

        var drawdown = input.MaximumDrawdownPercentage.Value;
        if (drawdown > -8m)
            return;

        findings.Add(new AnalyticsInsightFinding(
            "DRAWDOWN",
            AnalyticsInsightCategories.Risk,
            drawdown <= -15m
                ? AnalyticsInsightSeverities.Critical
                : AnalyticsInsightSeverities.Warning,
            MetricConfidence(input.QualityStatus),
            drawdown <= -15m ? 88 : 78,
            null,
            [new("maximumDrawdownPercentage", drawdown, "percentagePoints")]));
    }

    private static void AddAllocationFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (input.TargetPlanStatus != CompleteTargetPlan ||
            input.Allocation.Count == 0)
        {
            return;
        }

        var largestDrift = input.Allocation
            .Select(allocation => new
            {
                allocation.CategoryName,
                Drift = allocation.Percentage - allocation.TargetPercentage
            })
            .OrderByDescending(item => Math.Abs(item.Drift))
            .First();
        if (Math.Abs(largestDrift.Drift) <= input.AllocationTolerancePercentagePoints)
            return;

        var isLargeDrift = Math.Abs(largestDrift.Drift) >=
            input.AllocationTolerancePercentagePoints * 2m;
        findings.Add(new AnalyticsInsightFinding(
            "ALLOCATION_DRIFT",
            AnalyticsInsightCategories.Allocation,
            isLargeDrift
                ? AnalyticsInsightSeverities.Warning
                : AnalyticsInsightSeverities.Info,
            input.StaleAssetCount > 0
                ? AnalyticsInsightConfidences.Medium
                : AnalyticsInsightConfidences.High,
            isLargeDrift ? 74 : 58,
            largestDrift.CategoryName,
            [
                new("currentPercentage", input.Allocation
                    .First(item => item.CategoryName == largestDrift.CategoryName)
                    .Percentage, "percentagePoints"),
                new("targetPercentage", input.Allocation
                    .First(item => item.CategoryName == largestDrift.CategoryName)
                    .TargetPercentage, "percentagePoints"),
                new("driftPercentagePoints", largestDrift.Drift, "percentagePoints")
            ]));
    }

    private static void AddCashflowFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        var recentFlows = input.RecentMonthlyNetFlows.TakeLast(3).ToList();
        if (recentFlows.Count < 2)
            return;

        var negativeMonths = recentFlows.Count(flow => flow < 0m);
        if (negativeMonths < 2)
            return;

        findings.Add(new AnalyticsInsightFinding(
            "CASHFLOW_PRESSURE",
            AnalyticsInsightCategories.Cashflow,
            AnalyticsInsightSeverities.Warning,
            AnalyticsInsightConfidences.High,
            72,
            null,
            [
                new("negativeMonthCount", negativeMonths, "months"),
                new("recentNetFlow", recentFlows.Sum(), "money")
            ]));
    }

    private static void AddGoalFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (input.GoalAtRiskCount <= 0)
            return;

        findings.Add(new AnalyticsInsightFinding(
            "GOALS_AT_RISK",
            AnalyticsInsightCategories.Goals,
            AnalyticsInsightSeverities.Warning,
            AnalyticsInsightConfidences.High,
            68,
            null,
            [new("goalAtRiskCount", input.GoalAtRiskCount, "goals")]));
    }

    private static void AddDcaFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (input.DcaInsufficientCashCount <= 0)
            return;

        findings.Add(new AnalyticsInsightFinding(
            "DCA_CASH",
            AnalyticsInsightCategories.Goals,
            AnalyticsInsightSeverities.Info,
            AnalyticsInsightConfidences.High,
            56,
            null,
            [new("dcaInsufficientCashCount", input.DcaInsufficientCashCount, "plans")]));
    }

    private static void AddReturnGapFinding(
        AnalyticsInsightInput input,
        ICollection<AnalyticsInsightFinding> findings)
    {
        if (!input.TimeWeightedReturnPercentage.HasValue ||
            !input.MoneyWeightedReturnPercentage.HasValue)
        {
            return;
        }

        var gap = input.MoneyWeightedReturnPercentage.Value -
            input.TimeWeightedReturnPercentage.Value;
        if (Math.Abs(gap) < 5m)
            return;

        findings.Add(new AnalyticsInsightFinding(
            "RETURN_GAP",
            AnalyticsInsightCategories.Performance,
            AnalyticsInsightSeverities.Info,
            MetricConfidence(input.QualityStatus),
            46,
            null,
            [
                new("timeWeightedReturnPercentage", input.TimeWeightedReturnPercentage.Value, "percentagePoints"),
                new("moneyWeightedReturnPercentage", input.MoneyWeightedReturnPercentage.Value, "percentagePoints"),
                new("returnGapPercentagePoints", gap, "percentagePoints")
            ]));
    }

    private static string MetricConfidence(string qualityStatus) =>
        qualityStatus switch
        {
            CompleteQuality => AnalyticsInsightConfidences.High,
            "StalePrices" => AnalyticsInsightConfidences.Medium,
            _ => AnalyticsInsightConfidences.Low
        };
}
