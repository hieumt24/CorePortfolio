namespace CorePortfolio.Domain.Entities;

public sealed class SessionRefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserSessionId { get; set; }
    public UserSession UserSession { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? ReuseDetectedAt { get; set; }
}
