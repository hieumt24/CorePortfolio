namespace CorePortfolio.Domain.Entities;

public class Portfolio
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<PortfolioSnapshot> Snapshots { get; set; } = new List<PortfolioSnapshot>();
    public ICollection<CashflowRecord> CashflowRecords { get; set; } = new List<CashflowRecord>();
    public ICollection<CashAccount> CashAccounts { get; set; } = new List<CashAccount>();
}
