using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class ProfileTests
{
    [Fact]
    public async Task GetAndUpdateProfile_OnlyMutateCurrentUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var owner = CreatePasswordUser("profile-owner", "OldPassword123");
        var otherUser = CreatePasswordUser("profile-other", "OtherPassword123");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(owner, otherUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(owner.Id);
        var getResponse = await client.GetAsync("/api/profile", cancellationToken);
        var updateResponse = await client.PutAsJsonAsync(
            "/api/profile",
            new
            {
                username = "profile-renamed",
                displayName = "Nguyễn Minh",
                email = "minh@example.com"
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProfileResponse>(cancellationToken);
        Assert.Equal(owner.Id, Assert.IsType<ProfileResponse>(updated).Id);
        Assert.Equal("Nguyễn Minh", updated.DisplayName);
        Assert.Equal("minh@example.com", updated.Email);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedOwner = await verificationDb.Users.AsNoTracking()
            .SingleAsync(user => user.Id == owner.Id, cancellationToken);
        var storedOther = await verificationDb.Users.AsNoTracking()
            .SingleAsync(user => user.Id == otherUser.Id, cancellationToken);
        Assert.Equal("profile-renamed", storedOwner.Username);
        Assert.Equal("profile-other", storedOther.Username);
        Assert.Null(storedOther.Email);
    }

    [Fact]
    public async Task UpdateProfile_RejectsAnotherUsersEmail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var owner = CreatePasswordUser("email-owner", "OldPassword123");
        var otherUser = CreatePasswordUser("email-other", "OtherPassword123");
        otherUser.Email = "reserved@example.com";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(owner, otherUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(owner.Id);
        var response = await client.PutAsJsonAsync(
            "/api/profile",
            new
            {
                username = owner.Username,
                displayName = "Email Owner",
                email = "reserved@example.com"
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_RequiresCurrentPasswordAndInvalidatesOldPassword()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = CreatePasswordUser("password-owner", "OldPassword123");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var incorrectResponse = await client.PutAsJsonAsync(
            "/api/profile/password",
            new
            {
                currentPassword = "WrongPassword123",
                newPassword = "NewPassword456",
                confirmPassword = "NewPassword456"
            },
            cancellationToken);
        var successResponse = await client.PutAsJsonAsync(
            "/api/profile/password",
            new
            {
                currentPassword = "OldPassword123",
                newPassword = "NewPassword456",
                confirmPassword = "NewPassword456"
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, incorrectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, successResponse.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHash = await verificationDb.Users
            .Where(candidate => candidate.Id == user.Id)
            .Select(candidate => candidate.PasswordHash)
            .SingleAsync(cancellationToken);
        Assert.False(BCrypt.Net.BCrypt.Verify("OldPassword123", passwordHash));
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword456", passwordHash));
    }

    [Fact]
    public async Task AdminPasswordChange_DoesNotRestoreBootstrapPassword()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Users.SingleAsync(
                user => user.Username == "admin",
                cancellationToken);
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangedAdmin456");
            admin.IsActive = true;
            admin.Role = "Admin";
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateClient();
        var defaultPasswordResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "admin123" },
            cancellationToken);
        var changedPasswordResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "ChangedAdmin456" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, defaultPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, changedPasswordResponse.StatusCode);
    }

    private static User CreatePasswordUser(string username, string password) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        DisplayName = username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = "User",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private sealed record ProfileResponse(
        Guid Id,
        string Username,
        string DisplayName,
        string? Email,
        string Role,
        DateTime CreatedAt,
        DateTime? LastLoginAt);
}
