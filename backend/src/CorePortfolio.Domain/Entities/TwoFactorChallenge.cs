namespace CorePortfolio.Domain.Entities;

public sealed class TwoFactorChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public TwoFactorChallengePurpose Purpose { get; set; }
    public string? PendingSecretEncrypted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int FailedAttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public enum TwoFactorChallengePurpose
{
    Login = 0,
    Enrollment = 1
}
