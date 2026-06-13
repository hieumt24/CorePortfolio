namespace CorePortfolio.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal CurrentPrice { get; set; } // Cho phép cập nhật giá thủ công
    
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
}
