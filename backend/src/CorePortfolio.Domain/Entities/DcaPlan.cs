namespace CorePortfolio.Domain.Entities;

public class DcaPlan : IConcurrencyTracked
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;
    public Guid MarketAssetId { get; set; }
    public MarketAsset MarketAsset { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DcaFrequency Frequency { get; set; } = DcaFrequency.Monthly;
    public DateTime StartDate { get; set; }
    public DateTime NextExecutionDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; } = 1;
}

public enum DcaFrequency
{
    Weekly = 0,
    Monthly = 1,
    Quarterly = 2
}
