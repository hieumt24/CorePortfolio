using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class AdminControlPlaneTests
{
    [Fact]
    public async Task Auditor_CanReadButCannotExecuteOperations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var auditor = TestData.CreateUser("auditor-control-plane");
        auditor.Role = "Auditor";
        await SeedAsync(factory, cancellationToken, auditor);
        using var client = factory.CreateAuthenticatedClient(auditor.Id, auditor.Role);

        var read = await client.GetAsync("/api/admin/control-plane/data-integrity", cancellationToken);
        var execute = await client.PostAsync("/api/admin/control-plane/jobs/daily-snapshot/run", null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, execute.StatusCode);
    }

    [Fact]
    public async Task Broadcast_CreatesNotificationOnlyForActiveUsers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var admin = TestData.CreateUser("broadcast-admin");
        admin.Role = "Admin";
        var active = TestData.CreateUser("broadcast-active");
        var inactive = TestData.CreateUser("broadcast-inactive");
        inactive.IsActive = false;
        await SeedAsync(factory, cancellationToken, admin, active, inactive);
        using var client = factory.CreateAuthenticatedClient(admin.Id, admin.Role);

        var response = await client.PostAsJsonAsync(
            "/api/admin/control-plane/notification-campaigns",
            new { title = "Bảo trì", message = "Thông báo hệ thống", severity = 1, role = "User" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recipients = await db.Notifications.Select(item => item.UserId).ToListAsync(cancellationToken);
        Assert.Equal([active.Id], recipients);
    }

    [Fact]
    public async Task RevokeSession_ImmediatelyMarksRequestedSessionRevoked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var admin = TestData.CreateUser("session-admin");
        admin.Role = "Admin";
        var user = TestData.CreateUser("session-user");
        var session = new UserSession
        {
            UserId = user.Id,
            TokenId = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await SeedAsync(factory, cancellationToken, admin, user, session);
        using var client = factory.CreateAuthenticatedClient(admin.Id, admin.Role);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/control-plane/users/{user.Id}/sessions/revoke",
            new { sessionId = session.Id, reason = "Security review" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull((await db.UserSessions.SingleAsync(cancellationToken)).RevokedAt);
    }

    private static async Task SeedAsync(
        CorePortfolioApiFactory factory,
        CancellationToken cancellationToken,
        params object[] entities)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var entity in entities)
            db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
