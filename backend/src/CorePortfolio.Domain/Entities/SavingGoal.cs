namespace CorePortfolio.Domain.Entities;

public class SavingGoal : IConcurrencyTracked
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;
    public Guid? CashAccountId { get; set; }
    public CashAccount? CashAccount { get; set; }
    public Guid CashflowCategoryId { get; set; }
    public CashflowCategory CashflowCategory { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Version { get; set; } = 1;
}
