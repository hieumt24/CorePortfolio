namespace CorePortfolio.Domain.Entities;

public class CashAccount
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;
    public string Currency { get; set; } = "VND";
    public ICollection<CashLedgerEntry> Entries { get; set; } = new List<CashLedgerEntry>();
}
