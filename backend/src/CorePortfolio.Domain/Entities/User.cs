namespace CorePortfolio.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // e.g. "Admin", "User"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIpAddress { get; set; }
    public DateTime? LastActivityAt { get; set; }
    
    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
    public ICollection<CashflowRecord> CashflowRecords { get; set; } = new List<CashflowRecord>();
    public ICollection<CashflowCategory> CustomCategories { get; set; } = new List<CashflowCategory>();
    public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
    public ICollection<TargetAllocation> TargetAllocations { get; set; } = new List<TargetAllocation>();
}
