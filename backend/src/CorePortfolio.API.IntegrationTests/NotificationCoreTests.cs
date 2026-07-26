using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class NotificationCoreTests
{
    [Fact]
    public async Task ListAndUnreadCount_ReturnOnlyActiveNotificationsForCurrentUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("notification-owner");
        var otherUser = TestData.CreateUser("other-notification-owner");
        var visible = CreateNotification(user.Id, "budget:visible");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(user, otherUser);
            db.Notifications.AddRange(
                visible,
                CreateNotification(user.Id, "budget:read", readAt: DateTime.UtcNow),
                CreateNotification(user.Id, "budget:dismissed", dismissedAt: DateTime.UtcNow),
                CreateNotification(user.Id, "budget:expired", expiresAt: DateTime.UtcNow.AddMinutes(-1)),
                CreateNotification(otherUser.Id, "budget:other-user"));
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var listResponse = await client.GetAsync(
            "/api/notifications?unreadOnly=true&page=1&pageSize=10",
            cancellationToken);
        var countResponse = await client.GetAsync("/api/notifications/unread-count", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, countResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<NotificationPage>(cancellationToken);
        var item = Assert.Single(Assert.IsType<NotificationPage>(page).Items);
        Assert.Equal(visible.Id, item.Id);
        Assert.Equal("Budget", item.Type);
        Assert.Equal("Warning", item.Severity);
        var unread = await countResponse.Content.ReadFromJsonAsync<UnreadCount>(cancellationToken);
        Assert.Equal(1, Assert.IsType<UnreadCount>(unread).Count);
    }

    [Fact]
    public async Task MarkRead_CannotMutateAnotherUsersNotification()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("notification-reader");
        var otherUser = TestData.CreateUser("notification-protected-owner");
        var protectedNotification = CreateNotification(otherUser.Id, "budget:protected");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(user, otherUser);
            db.Notifications.Add(protectedNotification);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsync(
            $"/api/notifications/{protectedNotification.Id}/read",
            null,
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await verificationDb.Notifications
            .Where(notification => notification.Id == protectedNotification.Id)
            .Select(notification => notification.ReadAt)
            .SingleAsync(cancellationToken));
    }

    [Fact]
    public async Task Dismiss_MarksNotificationReadAndRemovesItFromActiveList()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("notification-dismisser");
        var notification = CreateNotification(user.Id, "budget:dismiss");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.DeleteAsync(
            $"/api/notifications/{notification.Id}",
            cancellationToken);
        var list = await client.GetFromJsonAsync<NotificationPage>(
            "/api/notifications",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(Assert.IsType<NotificationPage>(list).Items);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await verificationDb.Notifications.SingleAsync(cancellationToken);
        Assert.NotNull(stored.ReadAt);
        Assert.NotNull(stored.DismissedAt);
    }

    [Fact]
    public async Task Preferences_UpsertThresholdsAndSuppressNotificationWriter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("preference-owner");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var defaults = await client.GetFromJsonAsync<List<PreferenceResponse>>(
            "/api/notifications/preferences",
            cancellationToken);
        var defaultBudget = Assert.Single(Assert.IsType<List<PreferenceResponse>>(defaults), item => item.Type == "Budget");
        Assert.True(defaultBudget.IsEnabled);
        Assert.Equal(80m, defaultBudget.WarningThreshold);
        Assert.Equal(100m, defaultBudget.CriticalThreshold);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/notifications/preferences",
            new
            {
                preferences = new[]
                {
                    new
                    {
                        type = "Budget",
                        isEnabled = false,
                        warningThreshold = 75m,
                        criticalThreshold = 95m
                    }
                }
            },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var writerScope = factory.Services.CreateScope();
        var writer = writerScope.ServiceProvider.GetRequiredService<NotificationWriter>();
        var result = await writer.WriteAsync(
            new NotificationWriteRequest(
                user.Id,
                NotificationType.Budget,
                NotificationSeverity.Warning,
                "Ngân sách sắp đạt giới hạn",
                "Bạn đã sử dụng 75% ngân sách.",
                "budget:test:2026-07:75"),
            cancellationToken);
        Assert.Equal(NotificationWriteOutcome.Suppressed, result.Outcome);
        var writerDb = writerScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await writerDb.Notifications.AnyAsync(cancellationToken));
    }

    [Fact]
    public async Task NotificationWriter_ReturnsExistingNotificationForDuplicateDedupeKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("dedupe-owner");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        var writer = scope.ServiceProvider.GetRequiredService<NotificationWriter>();
        var request = new NotificationWriteRequest(
            user.Id,
            NotificationType.Dca,
            NotificationSeverity.Info,
            "Lịch DCA sắp đến",
            "Kế hoạch DCA sẽ đến hạn vào ngày mai.",
            "dca:test:due:2026-07-27",
            Link: "/dca-plans",
            EntityType: "DcaPlan",
            EntityId: Guid.NewGuid(),
            ActionLabel: "Xem kế hoạch",
            ExpiresAt: DateTime.UtcNow.AddDays(2),
            Metadata: new Dictionary<string, string?> { ["scheduledDate"] = "2026-07-27" });

        var created = await writer.WriteAsync(request, cancellationToken);
        var duplicate = await writer.WriteAsync(request, cancellationToken);

        Assert.Equal(NotificationWriteOutcome.Created, created.Outcome);
        Assert.Equal(NotificationWriteOutcome.Duplicate, duplicate.Outcome);
        Assert.Equal(created.NotificationId, duplicate.NotificationId);
        Assert.Equal(1, await db.Notifications.CountAsync(cancellationToken));
        var stored = await db.Notifications.SingleAsync(cancellationToken);
        Assert.Equal("DcaPlan", stored.EntityType);
        Assert.Contains("scheduledDate", stored.MetadataJson);
    }

    private static Notification CreateNotification(
        Guid userId,
        string dedupeKey,
        DateTime? readAt = null,
        DateTime? dismissedAt = null,
        DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = NotificationType.Budget,
        Severity = NotificationSeverity.Warning,
        Title = "Budget alert",
        Message = "Budget threshold reached.",
        DedupeKey = dedupeKey,
        CreatedAt = DateTime.UtcNow,
        ReadAt = readAt,
        DismissedAt = dismissedAt,
        ExpiresAt = expiresAt
    };

    private sealed record NotificationPage(
        List<NotificationResponse> Items,
        int TotalCount,
        int Page,
        int PageSize);

    private sealed record NotificationResponse(
        Guid Id,
        string Type,
        string Severity,
        string Title,
        string Message);

    private sealed record UnreadCount(int Count);

    private sealed record PreferenceResponse(
        string Type,
        bool IsEnabled,
        decimal? WarningThreshold,
        decimal? CriticalThreshold,
        DateTime? UpdatedAt);
}
