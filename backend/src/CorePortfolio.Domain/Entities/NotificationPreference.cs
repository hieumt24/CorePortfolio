namespace CorePortfolio.Domain.Entities;

public class NotificationPreference : IConcurrencyTracked
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}
