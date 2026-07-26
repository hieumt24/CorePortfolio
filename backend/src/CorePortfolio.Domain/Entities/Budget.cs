namespace CorePortfolio.Domain.Entities;

public class Budget : IConcurrencyTracked
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid CategoryId { get; set; }
    public CashflowCategory Category { get; set; } = null!;
    
    public decimal MonthlyLimit { get; set; }
    public int Version { get; set; } = 1;
}
