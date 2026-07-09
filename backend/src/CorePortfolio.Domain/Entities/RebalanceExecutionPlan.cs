namespace CorePortfolio.Domain.Entities;

public class RebalanceExecutionPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Currency { get; set; } = "VND";
    public RebalanceExecutionPlanStatus Status { get; set; } = RebalanceExecutionPlanStatus.Simulated;
    public decimal AvailableCash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public ICollection<RebalanceExecutionPlanItem> Items { get; set; } = new List<RebalanceExecutionPlanItem>();
}

public class RebalanceExecutionPlanItem
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public RebalanceExecutionPlan Plan { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public AssetCategory Category { get; set; } = null!;
    public RebalanceExecutionAction Action { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal SuggestedAmount { get; set; }
    public decimal ExecutableAmount { get; set; }
    public bool IsCashLimited { get; set; }
    public int Priority { get; set; }
}

public enum RebalanceExecutionPlanStatus
{
    Simulated = 0,
    Applied = 1
}

public enum RebalanceExecutionAction
{
    Buy = 0,
    Sell = 1
}
