namespace CorePortfolio.Domain.Performance;

public static class MoneyWeightedReturnCalculator
{
    private const double MinimumRate = -0.9999;
    private const double FunctionTolerance = 0.0000001;
    private const double RateTolerance = 0.0000001;
    private const int MaximumIterations = 200;

    private static readonly double[] CandidateRates =
    [
        MinimumRate, -0.99, -0.9, -0.75, -0.5, -0.25, 0,
        0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 50, 100
    ];

    public static PerformanceCalculationResult Calculate(IEnumerable<DatedCashFlow> source)
    {
        var cashFlows = source
            .GroupBy(flow => flow.Date.Date)
            .Select(group => new DatedCashFlow(group.Key, group.Sum(flow => flow.Amount)))
            .Where(flow => flow.Amount != 0)
            .OrderBy(flow => flow.Date)
            .ToList();

        if (cashFlows.Count < 2)
        {
            return PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.InsufficientData,
                "At least two non-zero dated cash flows are required.");
        }

        if (!cashFlows.Any(flow => flow.Amount < 0) ||
            !cashFlows.Any(flow => flow.Amount > 0))
        {
            return PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.InvalidData,
                "XIRR requires at least one positive and one negative cash flow.");
        }

        var firstDate = cashFlows[0].Date;
        double Evaluate(double rate) => cashFlows.Sum(flow =>
            (double)flow.Amount /
            Math.Pow(1d + rate, (flow.Date - firstDate).TotalDays / 365d));

        var brackets = new List<(double Lower, double Upper)>();
        var previousRate = CandidateRates[0];
        var previousValue = Evaluate(previousRate);

        for (var index = 1; index < CandidateRates.Length; index++)
        {
            var currentRate = CandidateRates[index];
            var currentValue = Evaluate(currentRate);

            if (Math.Abs(currentValue) <= FunctionTolerance)
                brackets.Add((currentRate, currentRate));
            else if (double.IsFinite(previousValue) &&
                     double.IsFinite(currentValue) &&
                     Math.Sign(previousValue) != Math.Sign(currentValue))
                brackets.Add((previousRate, currentRate));

            previousRate = currentRate;
            previousValue = currentValue;
        }

        if (brackets.Count == 0)
        {
            return PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.Unavailable,
                "XIRR did not converge within the supported rate range.");
        }

        if (brackets.Count > 1)
        {
            return PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.Unavailable,
                "XIRR has multiple possible solutions.");
        }

        var bracket = brackets[0];
        if (bracket.Lower == bracket.Upper)
            return PerformanceCalculationResult.Available((decimal)bracket.Lower);

        var lower = bracket.Lower;
        var upper = bracket.Upper;
        var lowerValue = Evaluate(lower);

        for (var iteration = 0; iteration < MaximumIterations; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var middleValue = Evaluate(middle);
            if (Math.Abs(middleValue) <= FunctionTolerance ||
                upper - lower <= RateTolerance)
                return PerformanceCalculationResult.Available((decimal)middle);

            if (Math.Sign(lowerValue) == Math.Sign(middleValue))
            {
                lower = middle;
                lowerValue = middleValue;
            }
            else
            {
                upper = middle;
            }
        }

        return PerformanceCalculationResult.Unavailable(
            PerformanceCalculationStatus.Unavailable,
            "XIRR did not converge.");
    }

    public static PerformanceCalculationResult CalculateFromPerformancePoints(
        IEnumerable<PerformancePoint> source)
    {
        var points = source.OrderBy(point => point.Date).ToList();
        if (points.Count < 2 || points[0].NetAssetValue <= 0)
        {
            return PerformanceCalculationResult.Unavailable(
                PerformanceCalculationStatus.InsufficientData,
                "At least two valuation points with a positive opening NAV are required.");
        }

        var cashFlows = new List<DatedCashFlow>
        {
            new(points[0].Date, -points[0].NetAssetValue)
        };
        cashFlows.AddRange(points
            .Skip(1)
            .Where(point => point.NetExternalFlow != 0)
            .Select(point => new DatedCashFlow(point.Date, -point.NetExternalFlow)));
        cashFlows.Add(new DatedCashFlow(points[^1].Date, points[^1].NetAssetValue));
        return Calculate(cashFlows);
    }
}
