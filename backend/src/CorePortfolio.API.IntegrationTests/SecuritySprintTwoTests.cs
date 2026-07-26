using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class SecuritySprintTwoTests
{
    [Fact]
    public async Task Login_IssuesSixtyMinuteAccessTokenAndStoresOnlyRefreshTokenHash()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = CreatePasswordUser("sprint-two-login", "StrongPassword123!");
        await SeedUserAsync(factory, user, cancellationToken);
        using var client = CreateCookieControlledClient(factory);

        var beforeLogin = DateTime.UtcNow;
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = user.Username, password = "StrongPassword123!" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.IsType<LoginResponse>(
            await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.InRange(jwt.ValidTo, beforeLogin.AddMinutes(59), beforeLogin.AddMinutes(61));
        Assert.InRange(result.ExpiresAt, beforeLogin.AddMinutes(59), beforeLogin.AddMinutes(61));

        var setCookie = GetRefreshSetCookie(response);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);
        var rawRefreshToken = ExtractRefreshToken(setCookie);

        using var verificationScope = factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedToken = await db.SessionRefreshTokens.AsNoTracking()
            .SingleAsync(item => item.UserSession.UserId == user.Id, cancellationToken);
        Assert.NotEqual(rawRefreshToken, storedToken.TokenHash);
        Assert.Equal(64, storedToken.TokenHash.Length);
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndReplayRevokesTheSession()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = CreatePasswordUser("sprint-two-rotation", "StrongPassword123!");
        await SeedUserAsync(factory, user, cancellationToken);
        using var client = CreateCookieControlledClient(factory);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = user.Username, password = "StrongPassword123!" },
            cancellationToken);
        var firstAccessToken = Assert.IsType<LoginResponse>(
            await login.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken)).Token;
        var firstRefreshToken = ExtractRefreshToken(GetRefreshSetCookie(login));

        var refresh = await PostWithRefreshCookieAsync(
            client,
            "/api/auth/refresh",
            firstRefreshToken,
            includeTrustedHeader: true,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var secondAccessToken = Assert.IsType<LoginResponse>(
            await refresh.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken)).Token;
        var secondRefreshToken = ExtractRefreshToken(GetRefreshSetCookie(refresh));
        Assert.NotEqual(firstAccessToken, secondAccessToken);
        Assert.NotEqual(firstRefreshToken, secondRefreshToken);

        var replay = await PostWithRefreshCookieAsync(
            client,
            "/api/auth/refresh",
            firstRefreshToken,
            includeTrustedHeader: true,
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        var rotatedTokenAfterReplay = await PostWithRefreshCookieAsync(
            client,
            "/api/auth/refresh",
            secondRefreshToken,
            includeTrustedHeader: true,
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, rotatedTokenAfterReplay.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.UserSessions.AsNoTracking()
            .SingleAsync(item => item.UserId == user.Id, cancellationToken);
        var tokens = await db.SessionRefreshTokens.AsNoTracking()
            .Where(item => item.UserSessionId == session.Id)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("Refresh token reuse detected", session.RevokeReason);
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].ConsumedAt);
        Assert.NotNull(tokens[0].ReuseDetectedAt);
        Assert.Equal(tokens[1].Id, tokens[0].ReplacedByTokenId);
    }

    [Fact]
    public async Task CookieSessionEndpoints_RejectRequestsWithoutCsrfHeader()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = CreatePasswordUser("sprint-two-csrf", "StrongPassword123!");
        await SeedUserAsync(factory, user, cancellationToken);
        using var client = CreateCookieControlledClient(factory);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = user.Username, password = "StrongPassword123!" },
            cancellationToken);
        var refreshToken = ExtractRefreshToken(GetRefreshSetCookie(login));

        var refresh = await PostWithRefreshCookieAsync(
            client,
            "/api/auth/refresh",
            refreshToken,
            includeTrustedHeader: false,
            cancellationToken);
        var logout = await PostWithRefreshCookieAsync(
            client,
            "/api/auth/logout",
            refreshToken,
            includeTrustedHeader: false,
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, logout.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedToken = await db.SessionRefreshTokens.AsNoTracking()
            .SingleAsync(item => item.UserSession.UserId == user.Id, cancellationToken);
        Assert.Null(storedToken.ConsumedAt);
        Assert.Null(storedToken.RevokedAt);
    }

    [Fact]
    public async Task PasswordChange_RevokesAllExistingRefreshSessions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = CreatePasswordUser("sprint-two-password", "OldPassword123!");
        await SeedUserAsync(factory, user, cancellationToken);
        using var client = CreateCookieControlledClient(factory);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, user.Id.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, user.Role);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = user.Username, password = "OldPassword123!" },
            cancellationToken);
        var refreshToken = ExtractRefreshToken(GetRefreshSetCookie(login));
        var changePassword = await client.PutAsJsonAsync(
            "/api/profile/password",
            new
            {
                currentPassword = "OldPassword123!",
                newPassword = "NewPassword456!",
                confirmPassword = "NewPassword456!"
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, changePassword.StatusCode);
        var refresh = await PostWithRefreshCookieAsync(
            client,
            "/api/auth/refresh",
            refreshToken,
            includeTrustedHeader: true,
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.UserSessions.AsNoTracking()
            .SingleAsync(item => item.UserId == user.Id, cancellationToken);
        var storedToken = await db.SessionRefreshTokens.AsNoTracking()
            .SingleAsync(item => item.UserSessionId == session.Id, cancellationToken);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("Password changed", session.RevokeReason);
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public async Task Login_RateLimitBlocksTheEleventhAttemptFromOneClientIp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        using var client = CreateCookieControlledClient(factory);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "rate-limited-user", password = "invalid-password" },
                cancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "rate-limited-user", password = "invalid-password" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    private static HttpClient CreateCookieControlledClient(CorePortfolioApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static async Task<HttpResponseMessage> PostWithRefreshCookieAsync(
        HttpClient client,
        string path,
        string refreshToken,
        bool includeTrustedHeader,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"coreportfolio.refresh={refreshToken}");
        if (includeTrustedHeader)
            request.Headers.Add("X-Requested-With", "CorePortfolio");
        return await client.SendAsync(request, cancellationToken);
    }

    private static string GetRefreshSetCookie(HttpResponseMessage response) =>
        Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("coreportfolio.refresh=", StringComparison.Ordinal));

    private static string ExtractRefreshToken(string setCookie) =>
        setCookie.Split(';', 2)[0].Split('=', 2)[1];

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

    private static async Task SeedUserAsync(
        CorePortfolioApiFactory factory,
        User user,
        CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record LoginResponse(
        string Token,
        DateTime ExpiresAt,
        Guid UserId,
        string Username,
        string? DisplayName,
        string? Email,
        string Role);
}
