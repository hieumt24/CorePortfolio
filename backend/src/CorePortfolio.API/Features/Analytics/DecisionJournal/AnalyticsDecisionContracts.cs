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
