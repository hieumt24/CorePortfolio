namespace CorePortfolio.Domain.Entities;

public class WatchlistItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid MarketAssetId { get; set; }
    public MarketAsset? MarketAsset { get; set; }
    
    public decimal? TargetPrice { get; set; }
    public DateTime AddedAt { get; set; }
}
