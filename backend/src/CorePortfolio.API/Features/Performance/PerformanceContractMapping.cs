using CorePortfolio.Domain.Performance;

namespace CorePortfolio.API.Features.Performance;

internal static class PerformanceContractMapping
{
    public static PerformanceMetricDto ToMetric(
        this PerformanceCalculationResult result,
        decimal multiplier = 1m) =>
        new(
            result.Value.HasValue ? result.Value.Value * multiplier : null,
            result.Status.ToString(),
            result.Reason);
}
