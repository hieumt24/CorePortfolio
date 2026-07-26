using CorePortfolio.Domain.Performance;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class PerformanceCalculatorTests
{
    [Fact]
    public void Twr_RemovesContributionFromInvestmentReturn()
    {
        var points = new[]
        {
            Point(2026, 1, 1, 100m),
            Point(2026, 1, 2, 220m, 100m),
            Point(2026, 1, 3, 242m)
        };

        var result = TimeWeightedReturnCalculator.Calculate(points);
        var absolute = TimeWeightedReturnCalculator.CalculateAbsoluteReturn(points);

        Assert.Equal(PerformanceCalculationStatus.Available, result.TotalReturn.Status);
        Assert.Equal(0.32m, result.TotalReturn.Value);
        Assert.Equal(42m, absolute.Value);
    }

    [Fact]
    public void Xirr_UsesActualDates()
    {
        var result = MoneyWeightedReturnCalculator.Calculate(
        [
            new DatedCashFlow(new DateTime(2025, 1, 1), -1_000m),
            new DatedCashFlow(new DateTime(2026, 1, 1), 1_100m)
        ]);

        Assert.Equal(PerformanceCalculationStatus.Available, result.Status);
        Assert.InRange(result.Value!.Value, 0.09999m, 0.10001m);
    }

    [Fact]
    public void Xirr_ReturnsUnavailableWhenCashFlowsHaveNoSignChange()
    {
        var result = MoneyWeightedReturnCalculator.Calculate(
        [
            new DatedCashFlow(new DateTime(2025, 1, 1), -1_000m),
            new DatedCashFlow(new DateTime(2026, 1, 1), -100m)
        ]);

        Assert.Equal(PerformanceCalculationStatus.InvalidData, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Drawdown_UsesFlowAdjustedGrowthIndex()
    {
        var result = DrawdownCalculator.Calculate(
        [
            Point(2026, 1, 1, 100m),
            Point(2026, 1, 2, 120m),
            Point(2026, 1, 3, 90m),
            Point(2026, 1, 4, 108m)
        ]);

        Assert.Equal(-0.25m, result.MaximumDrawdown.Value);
        Assert.Equal(-0.25m, result.Points[2].Drawdown);
    }

    [Fact]
    public void MonthlyReturns_ReportsBestWorstAndMonthlyVolatility()
    {
        var result = MonthlyReturnCalculator.Calculate(
        [
            Point(2025, 12, 31, 100m),
            Point(2026, 1, 31, 110m),
            Point(2026, 2, 28, 99m)
        ]);

        Assert.Equal(0.1m, result.BestMonth.Value);
        Assert.Equal(-0.1m, result.WorstMonth.Value);
        Assert.Equal(0.1m, result.MonthlyVolatility.Value);
    }

    [Fact]
    public void Calculators_ReturnDataStatusInsteadOfZeroForMissingSeries()
    {
        var twr = TimeWeightedReturnCalculator.Calculate(
        [
            Point(2026, 1, 1, 100m)
        ]);

        Assert.Equal(
            PerformanceCalculationStatus.InsufficientData,
            twr.TotalReturn.Status);
        Assert.Null(twr.TotalReturn.Value);
    }

    private static PerformancePoint Point(
        int year,
        int month,
        int day,
        decimal nav,
        decimal flow = 0) =>
        new(new DateTime(year, month, day), nav, flow, PortfolioSnapshotQuality.Complete);
}
