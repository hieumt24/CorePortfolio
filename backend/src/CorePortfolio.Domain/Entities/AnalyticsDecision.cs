namespace CorePortfolio.Domain.Entities;

public sealed class AnalyticsDecision : IConcurrencyTracked
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    public string PortfolioNameSnapshot { get; set; } = string.Empty;
    public AnalyticsDecisionType DecisionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string PlannedAction { get; set; } = string.Empty;
    public string RiskTriggers { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public AnalyticsDecisionStatus Status { get; set; } = AnalyticsDecisionStatus.Open;
    public AnalyticsDecisionOutcome? ReviewOutcome { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public DateTime ScopeFrom { get; set; }
    public DateTime ScopeTo { get; set; }
    public string Currency { get; set; } = "VND";
    public string DataQualityStatus { get; set; } = string.Empty;
    public decimal TrackedPortfolioValue { get; set; }
    public decimal? TimeWeightedReturnPercentage { get; set; }
    public decimal? MoneyWeightedReturnPercentage { get; set; }
    public decimal? MaximumDrawdownPercentage { get; set; }
    public string InsightCodes { get; set; } = string.Empty;
    public string MethodologyVersion { get; set; } = "decision-journal-v1";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int Version { get; set; } = 1;
}

public enum AnalyticsDecisionType
{
    Observation = 0,
    Allocation = 1,
    Cashflow = 2,
    Risk = 3,
    Goal = 4
}

public enum AnalyticsDecisionStatus
{
    Open = 0,
    Reviewed = 1
}

public enum AnalyticsDecisionOutcome
{
    OnTrack = 0,
    Adjust = 1,
    Closed = 2
}
