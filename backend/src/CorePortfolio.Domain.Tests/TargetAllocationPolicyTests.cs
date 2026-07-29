using CorePortfolio.Domain.Analytics;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class TargetAllocationPolicyTests
{
    [Fact]
    public void Evaluate_AllZero_ReturnsNotConfigured()
    {
        var result = TargetAllocationPolicy.Evaluate(
        [
            new TargetAllocationWeight(Guid.NewGuid(), 0m),
            new TargetAllocationWeight(Guid.NewGuid(), 0m)
        ]);

        Assert.Equal(TargetAllocationPlanStatuses.NotConfigured, result.Status);
        Assert.False(result.IsActionable);
        Assert.Equal(0m, result.TotalPercentage);
    }

    [Fact]
    public void Evaluate_CompletePlan_ReturnsActionable()
    {
        var result = TargetAllocationPolicy.Evaluate(
        [
            new TargetAllocationWeight(Guid.NewGuid(), 60m),
            new TargetAllocationWeight(Guid.NewGuid(), 40m)
        ]);

        Assert.Equal(TargetAllocationPlanStatuses.Complete, result.Status);
        Assert.True(result.IsActionable);
        Assert.Equal(100m, result.TotalPercentage);
        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Evaluate_OutOfRangePercentage_ReturnsInvalid(decimal percentage)
    {
        var result = TargetAllocationPolicy.Evaluate(
        [
            new TargetAllocationWeight(Guid.NewGuid(), percentage)
        ]);

        Assert.Equal(TargetAllocationPlanStatuses.Invalid, result.Status);
        Assert.False(result.IsActionable);
    }

    [Fact]
    public void Evaluate_IncompletePlan_ReturnsInvalid()
    {
        var result = TargetAllocationPolicy.Evaluate(
        [
            new TargetAllocationWeight(Guid.NewGuid(), 75m)
        ]);

        Assert.Equal(TargetAllocationPlanStatuses.Invalid, result.Status);
        Assert.False(result.IsActionable);
        Assert.Equal(75m, result.TotalPercentage);
    }

    [Fact]
    public void Evaluate_SmallNonZeroPlan_IsNotTreatedAsCleared()
    {
        var result = TargetAllocationPolicy.Evaluate(
        [
            new TargetAllocationWeight(Guid.NewGuid(), 0.005m)
        ]);

        Assert.Equal(TargetAllocationPlanStatuses.Invalid, result.Status);
        Assert.False(result.IsActionable);
    }

    [Fact]
    public void Evaluate_DuplicateCategory_ReturnsInvalid()
    {
        var categoryId = Guid.NewGuid();

        var result = TargetAllocationPolicy.Evaluate(
        [
            new TargetAllocationWeight(categoryId, 50m),
            new TargetAllocationWeight(categoryId, 50m)
        ]);

        Assert.Equal(TargetAllocationPlanStatuses.Invalid, result.Status);
        Assert.False(result.IsActionable);
    }

    [Theory]
    [InlineData(55, 50, 5, false)]
    [InlineData(55.01, 50, 5, true)]
    [InlineData(44.99, 50, 5, true)]
    public void IsOutsideTolerance_UsesPercentagePointBand(
        decimal current,
        decimal target,
        decimal tolerance,
        bool expected)
    {
        Assert.Equal(
            expected,
            TargetAllocationPolicy.IsOutsideTolerance(current, target, tolerance));
    }
}
