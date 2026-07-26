using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class SecuritySprintZeroTests
{
    [Fact]
    public async Task Login_DoesNotBootstrapDefaultAdmin()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        using var client = factory.CreateClient();
        int adminCountBefore;
        string? adminHashBefore;
        using (var setupScope = factory.Services.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            adminCountBefore = await setupDb.Users.CountAsync(
                item => item.Username.ToLower() == "admin",
                cancellationToken);
            adminHashBefore = await setupDb.Users
                .Where(item => item.Username.ToLower() == "admin")
                .Select(item => item.PasswordHash)
                .SingleOrDefaultAsync(cancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "admin123" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(adminCountBefore, await db.Users.CountAsync(
            item => item.Username.ToLower() == "admin",
            cancellationToken));
        Assert.Equal(adminHashBefore, await db.Users
            .Where(item => item.Username.ToLower() == "admin")
            .Select(item => item.PasswordHash)
            .SingleOrDefaultAsync(cancellationToken));
    }

    [Fact]
    public async Task Register_PersistsOnlyBcryptPasswordHash()
    {
        const string password = "correct-horse-battery-staple";
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { username = "password-storage-user", email = "secure@example.com", password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(
            item => item.Username == "password-storage-user",
            cancellationToken);
        Assert.NotEqual(password, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash, StringComparison.Ordinal);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
    }

    [Fact]
    public async Task UpdatePortfolio_DoesNotAllowCrossUserMutation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var owner = TestData.CreateUser("portfolio-owner");
        var attacker = TestData.CreateUser("portfolio-attacker");
        var portfolio = TestData.CreatePortfolio(owner, "Owner portfolio");
        await SeedAsync(factory, cancellationToken, owner, attacker, portfolio);
        using var client = factory.CreateAuthenticatedClient(attacker.Id);

        var response = await client.PutAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}",
            new { name = "Compromised", description = "Cross-user mutation" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Portfolios.SingleAsync(item => item.Id == portfolio.Id, cancellationToken);
        Assert.Equal("Owner portfolio", persisted.Name);
    }

    [Fact]
    public async Task DeleteAsset_DoesNotAllowCrossUserDeletion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var owner = TestData.CreateUser("asset-owner");
        var attacker = TestData.CreateUser("asset-attacker");
        Guid portfolioId;
        Guid assetId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seeded = await TestData.SeedTradableAssetAsync(db, owner, "Protected portfolio", cancellationToken);
            db.Users.Add(attacker);
            await db.SaveChangesAsync(cancellationToken);
            portfolioId = seeded.Portfolio.Id;
            assetId = seeded.Asset.Id;
        }
        using var client = factory.CreateAuthenticatedClient(attacker.Id);

        var response = await client.DeleteAsync(
            $"/api/portfolios/{portfolioId}/assets/{assetId}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await verificationDb.Assets.AnyAsync(item => item.Id == assetId, cancellationToken));
    }

    private static async Task SeedAsync(
        CorePortfolioApiFactory factory,
        CancellationToken cancellationToken,
        params object[] entities)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync(cancellationToken);
    }
}
