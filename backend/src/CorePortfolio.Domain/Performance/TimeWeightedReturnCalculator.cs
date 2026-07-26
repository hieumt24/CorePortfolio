namespace CorePortfolio.Domain.Performance;

public static class TimeWeightedReturnCalculator
{
    public static TimeWeightedReturnResult Calculate(IEnumerable<PerformancePoint> source)
    {
        var points = source
            .OrderBy(point => point.Date)
            .ToList();

        if (points.Count < 2)
        {
            return new TimeWeightedReturnResult(
                PerformanceCalculationResult.Unavailable(
                    PerformanceCalculationStatus.InsufficientData,
                    "At least two valuation points are required."),
                []);
        }

        if (points.Select(point => point.Date.Date).Distinct().Count() != points.Count)
        {
            return new TimeWeightedReturnResult(
                PerformanceCalculationResult.Unavailable(
                    PerformanceCalculationStatus.InvalidData,
                    "Valuation dates must be unique."),
                []);
        }

        decimal growthIndex = 100m;
        var periods = new List<PeriodReturn>(points.Count - 1);

        for (var index = 1; index < points.Count; index++)
        {
            var opening = points[index - 1];
            var closing = points[index];
            if (opening.NetAssetValue <= 0)
            {
                return new TimeWeightedReturnResult(
                    PerformanceCalculationResult.Unavailable(
                        PerformanceCalculationStatus.InvalidData,
                        $"Opening NAV must be positive on {opening.Date:yyyy-MM-dd}."),
                    periods);
            }

            var periodReturn =
                (closing.NetAssetValue - closing.NetExternalFlow) /
                opening.NetAssetValue - 1m;
            growthIndex *= 1m + periodReturn;
            periods.Add(new PeriodReturn(closing.Date, periodReturn, growthIndex));
        }

        return new TimeWeightedReturnResult(
            PerformanceCalculationResult.Available(growthIndex / 100m - 1m),
            periods);
    }

    public static PerformanceCalculationResult CalculateAbsoluteReturn(
        IEnumerable<PerformancePoint> source)
    {
        var points = source.OrderBy(point => point.Date).ToList();
        if (points.Count < 2)
        {
            return PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.InsufficientData,
                "At least two valuation points are required.");
        }

        var externalFlowsAfterOpening = points
            .Skip(1)
            .Sum(point => point.NetExternalFlow);
        return PerformanceCalculationResult.Available(
            points[^1].NetAssetValue -
            points[0].NetAssetValue -
            externalFlowsAfterOpening);
    }
}
