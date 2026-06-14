namespace CorePortfolio.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // e.g. "Admin", "User"
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}
