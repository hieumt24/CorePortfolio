namespace CorePortfolio.API.Features.Performance.Benchmarks;

public sealed record BenchmarkDefinitionDto(
    Guid Id,
    string Name,
    string Symbol,
    Guid? MarketAssetId,
    string AssetGroup,
    bool IsDefault,
    string Currency,
    bool IsActive,
    int PricePointCount,
    DateTime? LastPriceDate);

public sealed record BenchmarkComparisonPointDto(
    string Date,
    decimal PortfolioGrowthIndex,
    decimal? BenchmarkGrowthIndex,
    bool HasBenchmarkGap);

public sealed record BenchmarkComparisonDto(
    Guid BenchmarkId,
    string BenchmarkName,
    string BenchmarkSymbol,
    string BenchmarkCurrency,
    string? BaseDate,
    IReadOnlyList<BenchmarkComparisonPointDto> Points,
    int MissingBenchmarkDays,
    string QualityStatus,
    PerformanceQualityDto PortfolioQuality);
