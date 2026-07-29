using MediatR;

namespace CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;

public class RebalanceSuggestionDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal DifferenceValue { get; set; }
    public string Action { get; set; } = string.Empty; // "Increase" or "Reduce"
}

public sealed record RebalanceAssessmentDto(
    string TargetPlanStatus,
    decimal TotalTargetPercentage,
    decimal TolerancePercentagePoints,
    bool IsActionable,
    string? Reason,
    IReadOnlyList<RebalanceSuggestionDto> Suggestions);

public sealed record GetRebalanceSuggestionsQuery(string Currency = "VND")
    : IRequest<RebalanceAssessmentDto>;
