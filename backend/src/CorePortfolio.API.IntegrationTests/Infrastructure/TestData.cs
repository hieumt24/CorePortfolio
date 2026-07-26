using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;

namespace CorePortfolio.API.IntegrationTests.Infrastructure;

internal static class TestData
{
    public static User CreateUser(string username) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        PasswordHash = "not-used-by-integration-tests",
        Role = "User",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public static Portfolio CreatePortfolio(User user, string name) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        User = user,
        Name = name,
        Description = $"{name} integration-test portfolio",
        CreatedAt = DateTime.UtcNow
    };

    public static async Task<(Portfolio Portfolio, Asset Asset)> SeedTradableAssetAsync(
        AppDbContext db,
        User user,
        string portfolioName,
        CancellationToken cancellationToken)
    {
        var portfolio = CreatePortfolio(user, portfolioName);
        var marketAssetId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id,
            Portfolio = portfolio,
            MarketAssetId = marketAssetId
        };

        db.Users.Add(user);
        db.Portfolios.Add(portfolio);
        db.Assets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);
        return (portfolio, asset);
    }
}
