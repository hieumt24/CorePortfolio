namespace CorePortfolio.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }

    public Guid MarketAssetId { get; set; }
    public MarketAsset? MarketAsset { get; set; }
}
