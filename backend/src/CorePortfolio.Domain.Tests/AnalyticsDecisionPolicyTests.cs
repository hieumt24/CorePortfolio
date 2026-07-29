using CorePortfolio.Domain.Analytics;
using CorePortfolio.Domain.Entities;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class AnalyticsDecisionPolicyTests
{
    private static readonly DateTime Today =
        new(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, true)]
    [InlineData(30, true)]
    [InlineData(-1, false)]
    public void IsReviewDateAllowed_RejectsPastDates(
        int dayOffset,
        bool expected)
    {
        Assert.Equal(
            expected,
            AnalyticsDecisionPolicy.IsReviewDateAllowed(
                Today.AddDays(dayOffset),
                Today));
    }

    [Fact]
    public void IsReviewDateAllowed_RejectsDatesBeyondFiveYears()
    {
        Assert.False(AnalyticsDecisionPolicy.IsReviewDateAllowed(
            Today.AddYears(5).AddDays(1),
            Today));
    }

    [Theory]
    [InlineData(AnalyticsDecisionStatus.Open, -1, true)]
    [InlineData(AnalyticsDecisionStatus.Open, 0, false)]
    [InlineData(AnalyticsDecisionStatus.Reviewed, -1, false)]
    public void IsOverdue_RequiresOpenStatusAndPastReviewDate(
        AnalyticsDecisionStatus status,
        int dayOffset,
        bool expected)
    {
        Assert.Equal(
            expected,
            AnalyticsDecisionPolicy.IsOverdue(
                status,
                Today.AddDays(dayOffset),
                Today));
    }
}
