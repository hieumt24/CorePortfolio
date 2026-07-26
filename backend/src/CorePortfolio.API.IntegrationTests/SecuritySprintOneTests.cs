using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Admin.Categories;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class SecuritySprintOneTests
{
    [Fact]
    public async Task Auditor_CanReadMarketDataButCannotMutateItOrRepairIntegrity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var auditor = TestData.CreateUser("least-privilege-auditor");
        auditor.Role = "Auditor";
        await SeedUserAsync(factory, auditor, cancellationToken);
        using var client = factory.CreateAuthenticatedClient(auditor.Id, auditor.Role);

        var read = await client.GetAsync("/api/admin/market-assets?page=1&pageSize=1", cancellationToken);
        var mutate = await client.PostAsJsonAsync(
            "/api/admin/categories",
            new { name = "Auditor category", defaultCurrency = "VND" },
            cancellationToken);
        var repair = await client.PostAsJsonAsync(
            "/api/admin/control-plane/data-integrity/repair",
            new { checkKey = "ExpiredSessions", dryRun = false },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, mutate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, repair.StatusCode);
    }

    [Fact]
    public async Task MarketDataManager_CanCreateAssetCategory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var manager = TestData.CreateUser("market-data-manager");
        manager.Role = "MarketDataManager";
        await SeedUserAsync(factory, manager, cancellationToken);
        using var client = factory.CreateAuthenticatedClient(manager.Id, manager.Role);

        var response = await client.PostAsJsonAsync(
            "/api/admin/categories",
            new { name = "Security Sprint One", defaultCurrency = "VND" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Support_CannotCreateGlobalCashflowCategory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var support = TestData.CreateUser("global-category-support");
        support.Role = "Support";
        await SeedUserAsync(factory, support, cancellationToken);
        using var client = factory.CreateAuthenticatedClient(support.Id, support.Role);

        var response = await client.PostAsJsonAsync(
            "/api/cashflows/categories",
            new
            {
                name = "Forbidden global category",
                type = 1,
                icon = "shield",
                color = "#000000",
                isGlobal = true,
                sortOrder = 0
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegularUser_CanReadSharedMarketAssetsButCannotOpenAdminCapabilities()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("non-admin-capabilities");
        await SeedUserAsync(factory, user, cancellationToken);
        using var client = factory.CreateAuthenticatedClient(user.Id, user.Role);

        var sharedRead = await client.GetAsync("/api/admin/market-assets?page=1&pageSize=1", cancellationToken);
        var response = await client.GetAsync("/api/admin/control-plane/capabilities", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, sharedRead.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MediatRPipeline_RejectsDirectMutationWithoutRequiredPermission()
    {
        using var factory = new CorePortfolioApiFactory();
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Auditor")
            ], "SecuritySprintOneTest"))
        };
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => sender.Send(
            new CreateCategoryCommand("Pipeline blocked category", "VND"),
            TestContext.Current.CancellationToken));

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.AssetCategories.AnyAsync(
            item => item.Name == "Pipeline blocked category",
            TestContext.Current.CancellationToken));
    }

    private static async Task SeedUserAsync(
        CorePortfolioApiFactory factory,
        CorePortfolio.Domain.Entities.User user,
        CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
