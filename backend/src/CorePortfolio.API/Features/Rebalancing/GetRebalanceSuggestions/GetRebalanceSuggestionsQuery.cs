using MediatR;

namespace CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;

public class RebalanceSuggestionDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal DifferenceValue { get; set; }
    public string Action { get; set; } = string.Empty; // "Buy", "Sell", "Hold"
}

public class GetRebalanceSuggestionsQuery : IRequest<List<RebalanceSuggestionDto>>
{
    public Guid UserId { get; set; }
    public string Currency { get; set; } = "VND";
    public GetRebalanceSuggestionsQuery(Guid userId, string currency)
    {
        UserId = userId;
        Currency = currency;
    }
}
