namespace CorePortfolio.API.Features.Performance;

public sealed record PerformanceDataQualityDto(
    DateTime From,
    DateTime To,
    DateTime? AsOf,
    string QualityStatus,
    int PortfolioCount,
    int SnapshotCount,
    int ExpectedSnapshotCount,
    int MissingSnapshotCount,
    int MissingSnapshotDays,
    IReadOnlyList<string> MissingDates,
    int StaleAssetCount,
    int UnclassifiedCashFlowCount,
    IReadOnlyList<string> Issues);

public sealed record PerformanceMetricDto(
    decimal? Value,
    string Status,
    string? Reason);

public sealed record PerformanceQualityDto(
    DateTime? AsOf,
    string QualityStatus,
    int MissingSnapshotDays,
    int StaleAssetCount,
    int UnclassifiedCashFlowCount);

public sealed record PerformanceSummaryDto(
    string Currency,
    DateTime From,
    DateTime To,
    decimal StartingNetAssetValue,
    decimal EndingNetAssetValue,
    decimal NetExternalFlow,
    PerformanceMetricDto AbsoluteReturn,
    PerformanceMetricDto TimeWeightedReturnPercentage,
    PerformanceMetricDto MoneyWeightedReturnPercentage,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    decimal TotalPnl,
    PerformanceMetricDto MaximumDrawdownPercentage,
    PerformanceMetricDto BestMonthPercentage,
    PerformanceMetricDto WorstMonthPercentage,
    PerformanceMetricDto MonthlyVolatilityPercentage,
    PerformanceQualityDto Quality);

public sealed record PerformanceSeriesPointDto(
    string Date,
    decimal NetAssetValue,
    decimal NetExternalFlow,
    decimal CumulativeExternalFlow,
    decimal? PeriodReturnPercentage,
    decimal GrowthIndex,
    string QualityStatus);

public sealed record PerformanceSeriesDto(
    string Currency,
    DateTime From,
    DateTime To,
    IReadOnlyList<PerformanceSeriesPointDto> Points,
    PerformanceQualityDto Quality);

public sealed record PerformanceDrawdownPointDto(
    string Date,
    decimal GrowthIndex,
    decimal PeakGrowthIndex,
    decimal DrawdownPercentage);

public sealed record PerformanceDrawdownSeriesDto(
    DateTime From,
    DateTime To,
    PerformanceMetricDto MaximumDrawdownPercentage,
    IReadOnlyList<PerformanceDrawdownPointDto> Points,
    PerformanceQualityDto Quality);

public sealed record PerformanceMonthlyReturnDto(
    string Month,
    decimal? ReturnPercentage,
    string Status,
    string? Reason);

public sealed record PerformanceMonthlyReturnsDto(
    DateTime From,
    DateTime To,
    IReadOnlyList<PerformanceMonthlyReturnDto> Months,
    PerformanceMetricDto BestMonthPercentage,
    PerformanceMetricDto WorstMonthPercentage,
    PerformanceMetricDto MonthlyVolatilityPercentage,
    PerformanceQualityDto Quality);
