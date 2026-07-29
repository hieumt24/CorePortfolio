using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Analytics;

namespace CorePortfolio.API.Features.Analytics.DecisionJournal;

public sealed record CreateAnalyticsDecisionRequest(
    Guid? PortfolioId,
    DateTime? From,
    DateTime? To,
    string Currency,
    string DecisionType,
    string Title,
    string Rationale,
    string PlannedAction,
    string RiskTriggers,
    DateTime ReviewDate);

public sealed record ReviewAnalyticsDecisionRequest(
    string Outcome,
    string Notes);

public sealed record AnalyticsDecisionSnapshotDto(
    DateTime From,
    DateTime To,
    string Currency,
    string DataQualityStatus,
    decimal TrackedPortfolioValue,
    decimal? TimeWeightedReturnPercentage,
    decimal? MoneyWeightedReturnPercentage,
    decimal? MaximumDrawdownPercentage,
    IReadOnlyList<string> InsightCodes,
    string MethodologyVersion);

public sealed record AnalyticsDecisionDto(
    Guid Id,
    Guid? PortfolioId,
    bool IsPortfolioScope,
    string PortfolioName,
    string DecisionType,
    string Title,
    string Rationale,
    string PlannedAction,
    string RiskTriggers,
    DateTime ReviewDate,
    string Status,
    string? ReviewOutcome,
    string ReviewNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ReviewedAt,
    bool IsOverdue,
    AnalyticsDecisionSnapshotDto Snapshot);

internal static class AnalyticsDecisionMapper
{
    public static AnalyticsDecisionDto ToDto(
        AnalyticsDecision decision,
        DateTime utcToday)
    {
        var insightCodes = decision.InsightCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new AnalyticsDecisionDto(
            decision.Id,
            decision.PortfolioId,
            decision.IsPortfolioScope,
            decision.PortfolioNameSnapshot,
            decision.DecisionType.ToString(),
            decision.Title,
            decision.Rationale,
            decision.PlannedAction,
            decision.RiskTriggers,
            decision.ReviewDate,
            decision.Status.ToString(),
            decision.ReviewOutcome?.ToString(),
            decision.ReviewNotes,
            decision.CreatedAt,
            decision.UpdatedAt,
            decision.ReviewedAt,
            AnalyticsDecisionPolicy.IsOverdue(
                decision.Status,
                decision.ReviewDate,
                utcToday),
            new AnalyticsDecisionSnapshotDto(
                decision.ScopeFrom,
                decision.ScopeTo,
                decision.Currency,
                decision.DataQualityStatus,
                decision.TrackedPortfolioValue,
                decision.TimeWeightedReturnPercentage,
                decision.MoneyWeightedReturnPercentage,
                decision.MaximumDrawdownPercentage,
                insightCodes,
                decision.MethodologyVersion));
    }
}

public sealed record AnalyticsDecisionMetricComparisonDto(
    decimal? Baseline,
    decimal? Current,
    decimal? Delta);

public sealed record AnalyticsDecisionReviewComparisonDto(
    string Readiness,
    string Confidence,
    AnalyticsDecisionMetricComparisonDto TrackedPortfolioValue,
    decimal? TrackedPortfolioValueChangePercentage,
    AnalyticsDecisionMetricComparisonDto TimeWeightedReturnPercentage,
    AnalyticsDecisionMetricComparisonDto MoneyWeightedReturnPercentage,
    AnalyticsDecisionMetricComparisonDto MaximumDrawdownPercentage,
    IReadOnlyList<string> NewInsightCodes,
    IReadOnlyList<string> ResolvedInsightCodes,
    IReadOnlyList<string> PersistentInsightCodes);

public sealed record AnalyticsDecisionReviewContextDto(
    Guid DecisionId,
    DateTime GeneratedAt,
    string MethodologyVersion,
    string? Reason,
    AnalyticsDecisionSnapshotDto Baseline,
    AnalyticsDecisionSnapshotDto? Current,
    AnalyticsDecisionReviewComparisonDto Comparison,
    string Disclaimer);
