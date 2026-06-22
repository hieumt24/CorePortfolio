namespace CorePortfolio.Domain.Entities;

public class TargetAllocation
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public AssetCategory Category { get; set; } = null!;

    public decimal TargetPercentage { get; set; }
}
