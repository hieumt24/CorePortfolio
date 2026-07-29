using CorePortfolio.Domain.Entities;

namespace CorePortfolio.Domain.Analytics;

public static class AnalyticsDecisionPolicy
{
    public const int MaximumReviewHorizonYears = 5;

    public static DateTime NormalizeDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    public static bool IsReviewDateAllowed(
        DateTime reviewDate,
        DateTime utcToday)
    {
        var normalizedReviewDate = NormalizeDate(reviewDate);
        var normalizedToday = NormalizeDate(utcToday);
        return normalizedReviewDate >= normalizedToday &&
            normalizedReviewDate <= normalizedToday.AddYears(MaximumReviewHorizonYears);
    }

    public static bool IsOverdue(
        AnalyticsDecisionStatus status,
        DateTime reviewDate,
        DateTime utcToday) =>
        status == AnalyticsDecisionStatus.Open &&
        NormalizeDate(reviewDate) < NormalizeDate(utcToday);
}
