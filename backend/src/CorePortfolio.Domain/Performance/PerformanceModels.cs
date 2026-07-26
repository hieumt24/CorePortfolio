namespace CorePortfolio.Domain.Performance;

public sealed record PerformancePoint(
    DateTime Date,
    decimal NetAssetValue,
    decimal NetExternalFlow,
    string QualityStatus);

public sealed record DatedCashFlow(DateTime Date, decimal Amount);

public enum PerformanceCalculationStatus
{
    Available = 0,
    InsufficientData = 1,
    InvalidData = 2,
    Unavailable = 3
}

public sealed record PerformanceCalculationResult(
    decimal? Value,
    PerformanceCalculationStatus Status,
    string? Reason = null)
{
    public static PerformanceCalculationResult Available(decimal value) =>
        new(value, PerformanceCalculationStatus.Available);

    public static PerformanceCalculationResult Unavailable(
        PerformanceCalculationStatus status,
        string reason) =>
        new(null, status, reason);
}

public sealed record PeriodReturn(
    DateTime Date,
    decimal Return,
    decimal GrowthIndex);

public sealed record TimeWeightedReturnResult(
    PerformanceCalculationResult TotalReturn,
    IReadOnlyList<PeriodReturn> Periods);

public sealed record DrawdownPoint(
    DateTime Date,
    decimal GrowthIndex,
    decimal PeakGrowthIndex,
    decimal Drawdown);

public sealed record DrawdownResult(
    PerformanceCalculationResult MaximumDrawdown,
    IReadOnlyList<DrawdownPoint> Points);

public sealed record MonthlyReturn(
    DateTime Month,
    decimal? Return,
    PerformanceCalculationStatus Status,
    string? Reason);

public sealed record MonthlyReturnResult(
    IReadOnlyList<MonthlyReturn> Returns,
    PerformanceCalculationResult BestMonth,
    PerformanceCalculationResult WorstMonth,
    PerformanceCalculationResult MonthlyVolatility);
