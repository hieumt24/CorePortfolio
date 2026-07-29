namespace CorePortfolio.API.Features.Rebalancing;

public sealed class RebalancingOptions
{
    public const string SectionName = "Analytics:Rebalancing";

    public decimal TolerancePercentagePoints { get; init; } = 5m;
}
