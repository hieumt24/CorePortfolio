namespace CorePortfolio.Domain.Entities;

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevokeReason { get; set; }
    public DateTime? TwoFactorVerifiedAt { get; set; }
    public string AuthenticationMethod { get; set; } = "pwd";
    public ICollection<SessionRefreshToken> RefreshTokens { get; set; } = new List<SessionRefreshToken>();
}
