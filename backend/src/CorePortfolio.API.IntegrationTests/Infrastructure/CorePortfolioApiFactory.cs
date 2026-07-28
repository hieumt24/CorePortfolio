using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.API.IntegrationTests.Infrastructure;

public sealed class CorePortfolioApiFactory(
    bool enforceTwoFactorForPrivilegedRoles = false,
    string? twoFactorEncryptionKey = null)
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:DefaultConnection", _connection.ConnectionString)
            .UseSetting("Jwt:Key", "integration-test-key-with-at-least-32-characters")
            .UseSetting("Jwt:Issuer", "CorePortfolio.IntegrationTests")
            .UseSetting("Jwt:Audience", "CorePortfolio.IntegrationTests")
            .UseSetting("MarketPrices:Enabled", "false")
            .UseSetting("Telegram:Enabled", "false")
            .UseSetting(
                "Security:TwoFactor:EncryptionKey",
                twoFactorEncryptionKey ??
                "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=")
            .UseSetting(
                "Security:TwoFactor:EnforceForPrivilegedRoles",
                enforceTwoFactorForPrivilegedRoles.ToString());

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddSingleton(_connection);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            var backgroundServices = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType is not null &&
                    (descriptor.ImplementationType == typeof(TelegramCronService) ||
                     descriptor.ImplementationType == typeof(DailySnapshotService) ||
                     descriptor.ImplementationType == typeof(MarketPriceRefreshService) ||
                     descriptor.ImplementationType == typeof(ScheduledBackupService) ||
                     descriptor.ImplementationType == typeof(
                         CorePortfolio.API.Features.Auth.TwoFactor.TwoFactorChallengeCleanupService)))
                .ToList();
            foreach (var descriptor in backgroundServices)
                services.Remove(descriptor);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(
        Guid userId,
        string role = "User",
        bool mfaVerified = true)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.MfaHeader,
            mfaVerified.ToString());
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
