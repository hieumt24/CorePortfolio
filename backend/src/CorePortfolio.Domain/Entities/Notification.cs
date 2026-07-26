namespace CorePortfolio.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; } = NotificationType.System;
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string? DedupeKey { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? ActionLabel { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public enum NotificationType
{
    System = 0,
    Budget = 1,
    SavingGoal = 2,
    Dca = 3,
    Rebalancing = 4,
    MarketPrice = 5,
    RecurringCashflow = 6
}

public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
