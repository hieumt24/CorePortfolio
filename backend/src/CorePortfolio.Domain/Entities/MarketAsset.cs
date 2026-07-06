namespace CorePortfolio.Domain.Entities;

public class MarketAsset
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public AssetCategory? Category { get; set; }
    
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public DateTime LastUpdated { get; set; }
    public string PriceSource { get; set; } = "Manual";
    public string? ExternalId { get; set; }
    public string PriceStatus { get; set; } = "Manual";
    public string? LastPriceError { get; set; }
}
