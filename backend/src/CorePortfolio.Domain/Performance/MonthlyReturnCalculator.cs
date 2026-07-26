namespace CorePortfolio.Domain.Performance;

public static class MonthlyReturnCalculator
{
    public static MonthlyReturnResult Calculate(IEnumerable<PerformancePoint> source)
    {
        var points = source.OrderBy(point => point.Date).ToList();
        if (points.Count < 2)
        {
            var unavailable = PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.InsufficientData,
                "At least two valuation points are required.");
            return new MonthlyReturnResult([], unavailable, unavailable, unavailable);
        }

        var monthlyReturns = new List<MonthlyReturn>();
        foreach (var monthGroup in points.GroupBy(point =>
                     new DateTime(point.Date.Year, point.Date.Month, 1)))
        {
            var firstIndex = points.FindIndex(point => point == monthGroup.First());
            var calculationPoints = firstIndex > 0
                ? points.Skip(firstIndex - 1).Take(monthGroup.Count() + 1)
                : monthGroup;
            var result = TimeWeightedReturnCalculator.Calculate(calculationPoints);
            monthlyReturns.Add(new MonthlyReturn(
                monthGroup.Key,
                result.TotalReturn.Value,
                result.TotalReturn.Status,
                result.TotalReturn.Reason));
        }

        var available = monthlyReturns
            .Where(item => item.Status == PerformanceCalculationStatus.Available &&
                           item.Return.HasValue)
            .ToList();
        if (available.Count == 0)
        {
            var unavailable = PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.InsufficientData,
                "No complete monthly return is available.");
            return new MonthlyReturnResult(monthlyReturns, unavailable, unavailable, unavailable);
        }

        var best = available.MaxBy(item => item.Return)!.Return!.Value;
        var worst = available.MinBy(item => item.Return)!.Return!.Value;
        var average = available.Average(item => item.Return!.Value);
        var variance = available.Average(item =>
        {
            var difference = item.Return!.Value - average;
            return difference * difference;
        });
        var volatility = (decimal)Math.Sqrt((double)variance);

        return new MonthlyReturnResult(
            monthlyReturns,
            PerformanceCalculationResult.Available(best),
            PerformanceCalculationResult.Available(worst),
            PerformanceCalculationResult.Available(volatility));
    }
}
