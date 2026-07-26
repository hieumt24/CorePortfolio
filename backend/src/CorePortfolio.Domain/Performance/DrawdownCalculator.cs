namespace CorePortfolio.Domain.Performance;

public static class DrawdownCalculator
{
    public static DrawdownResult Calculate(IEnumerable<PerformancePoint> source)
    {
        var points = source.OrderBy(point => point.Date).ToList();
        var timeWeightedReturn = TimeWeightedReturnCalculator.Calculate(points);
        if (timeWeightedReturn.TotalReturn.Status != PerformanceCalculationStatus.Available)
        {
            return new DrawdownResult(
                timeWeightedReturn.TotalReturn,
                []);
        }

        decimal peak = 100m;
        decimal maximumDrawdown = 0m;
        var drawdowns = new List<DrawdownPoint>
        {
            new(points[0].Date, 100m, 100m, 0m)
        };

        foreach (var period in timeWeightedReturn.Periods)
        {
            peak = Math.Max(peak, period.GrowthIndex);
            var drawdown = peak == 0 ? 0 : period.GrowthIndex / peak - 1m;
            maximumDrawdown = Math.Min(maximumDrawdown, drawdown);
            drawdowns.Add(new DrawdownPoint(
                period.Date,
                period.GrowthIndex,
                peak,
                drawdown));
        }

        return new DrawdownResult(
            PerformanceCalculationResult.Available(maximumDrawdown),
            drawdowns);
    }
}
