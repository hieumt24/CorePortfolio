namespace CorePortfolio.Domain.Entities;

public class CashflowRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }

    public Guid CategoryId { get; set; }
    public CashflowCategory? Category { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND"; // e.g. VND, USD
    
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;

    // Optional: link to the transaction generated in the portfolio
    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
}
