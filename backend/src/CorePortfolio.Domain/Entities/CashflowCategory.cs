namespace CorePortfolio.Domain.Entities;

public class CashflowCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public CashflowType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsGlobal { get; set; } // True if seeded by Admin
    public Guid? UserId { get; set; } // Null if IsGlobal
    public User? User { get; set; }
}
