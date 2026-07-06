namespace CorePortfolio.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    
    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }
    
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Fee { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
