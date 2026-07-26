using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class UserIsolationTests
{
    [Fact]
    public async Task GetPortfolios_ReturnsOnlyAuthenticatedUsersPortfolios()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var firstUser = TestData.CreateUser("first-user");
        var secondUser = TestData.CreateUser("second-user");
        var firstPortfolio = TestData.CreatePortfolio(firstUser, "Visible portfolio");
        var secondPortfolio = TestData.CreatePortfolio(secondUser, "Hidden portfolio");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(firstUser, secondUser, firstPortfolio, secondPortfolio);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(firstUser.Id);
        var response = await client.GetAsync("/api/portfolios", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var portfolios = await response.Content.ReadFromJsonAsync<List<PortfolioResponse>>(cancellationToken);
        var portfolio = Assert.Single(Assert.IsType<List<PortfolioResponse>>(portfolios));
        Assert.Equal(firstPortfolio.Id, portfolio.Id);
        Assert.Equal("Visible portfolio", portfolio.Name);
    }

    private sealed record PortfolioResponse(Guid Id, string Name, string Description, DateTime CreatedAt);
}
