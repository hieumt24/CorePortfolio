namespace CorePortfolio.Domain.Entities;

public sealed class BenchmarkDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public Guid? MarketAssetId { get; set; }
    public MarketAsset? MarketAsset { get; set; }
    public string AssetGroup { get; set; } = "All";
    public bool IsDefault { get; set; }
    public string Currency { get; set; } = "VND";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<BenchmarkPricePoint> PricePoints { get; set; } =
        new List<BenchmarkPricePoint>();
}
