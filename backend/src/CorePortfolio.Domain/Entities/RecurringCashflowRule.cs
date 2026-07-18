namespace CorePortfolio.Domain.Entities;

public class RecurringCashflowRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public CashflowCategory Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Frequency { get; set; } = "Monthly";
    public DateTime NextOccurrence { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastGeneratedAt { get; set; }
}
