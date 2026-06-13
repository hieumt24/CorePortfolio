namespace CorePortfolio.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty; // e.g., Stock, Crypto, MutualFund
    
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
}
