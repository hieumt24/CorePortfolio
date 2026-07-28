using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.API.Features.Auth.TwoFactor;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Claims;
using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Admin.ControlPlane;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CorePortfolio.API.IntegrationTests;

public sealed class TwoFactorAuthenticationTests
{
    [Fact]
    public void TotpService_MatchesRfc6238Sha1Vector()
    {
        var service = new TotpService(Options.Create(new TwoFactorOptions()));

        var code = service.ComputeCode(
            "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            DateTimeOffset.FromUnixTimeSeconds(59).UtcDateTime);

        Assert.Equal("287082", code);
    }

    [Fact]
    public void SecretProtector_EncryptsAndBindsCiphertextToUser()
    {
        var options = Options.Create(new TwoFactorOptions
        {
            EncryptionKey = "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY="
        });
        var protector = new TwoFactorSecretProtector(options);
        var userId = Guid.NewGuid();

        var encrypted = protector.Protect("TOTP-SECRET", userId);

        Assert.DoesNotContain("TOTP-SECRET", encrypted);
        Assert.Equal("TOTP-SECRET", protector.Unprotect(encrypted, userId));
        Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(encrypted, Guid.NewGuid()));
    }

    [Fact]
    public void TwoFactorResetPermission_IsRestrictedToSuperAdmin()
    {
        Assert.True(AdminPermissionCatalog.Has(
            "SuperAdmin",
            AdminPermissionCatalog.TwoFactorReset));
        Assert.False(AdminPermissionCatalog.Has(
            "Admin",
            AdminPermissionCatalog.TwoFactorReset));
    }

    [Fact]
    public async Task PrivilegedLogin_DoesNotIssueTokenCookieOrSessionBeforeEnrollment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "mfa-admin", "Admin", cancellationToken);
        using var client = factory.CreateClient();

        var response = await LoginAsync(client, admin.Username, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.IsType<LoginResponse>(
            await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken));
        Assert.Equal("TwoFactorSetupRequired", result.Status);
        Assert.Null(result.Token);
        Assert.False(string.IsNullOrWhiteSpace(result.ChallengeToken));
        Assert.DoesNotContain(
            response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
            value => value.StartsWith("coreportfolio.refresh=", StringComparison.Ordinal));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.UserSessions.ToListAsync(cancellationToken));
        Assert.Empty(await db.SessionRefreshTokens.ToListAsync(cancellationToken));
        var challenge = Assert.Single(await db.TwoFactorChallenges.ToListAsync(cancellationToken));
        Assert.NotEqual(result.ChallengeToken, challenge.TokenHash);
        Assert.Equal(
            TwoFactorChallengeService.HashToken(result.ChallengeToken!),
            challenge.TokenHash);
    }

    [Fact]
    public async Task Enrollment_VerifiesTotpAndStoresOnlyEncryptedSecretAndRecoveryHashes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "enroll-admin", "SuperAdmin", cancellationToken);
        using var client = factory.CreateClient();

        var enrollment = await EnrollAsync(factory, client, admin.Username, cancellationToken);

        Assert.Equal("Authenticated", enrollment.Login.Status);
        Assert.False(string.IsNullOrWhiteSpace(enrollment.Login.Token));
        Assert.Equal(10, enrollment.Login.RecoveryCodes?.Count);
        Assert.Contains(
            enrollment.VerifyResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("coreportfolio.refresh=", StringComparison.Ordinal));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedUser = await db.Users.SingleAsync(item => item.Id == admin.Id, cancellationToken);
        Assert.True(storedUser.TwoFactorEnabled);
        Assert.NotNull(storedUser.TwoFactorEnabledAt);
        Assert.NotEqual(enrollment.Setup.ManualKey, storedUser.TwoFactorSecretEncrypted);
        Assert.DoesNotContain(enrollment.Setup.ManualKey, storedUser.TwoFactorSecretEncrypted!);

        var session = Assert.Single(await db.UserSessions.ToListAsync(cancellationToken));
        Assert.Equal("otp", session.AuthenticationMethod);
        Assert.NotNull(session.TwoFactorVerifiedAt);
        var challenge = Assert.Single(await db.TwoFactorChallenges.ToListAsync(cancellationToken));
        Assert.NotNull(challenge.ConsumedAt);

        var storedCodes = await db.TwoFactorRecoveryCodes.ToListAsync(cancellationToken);
        Assert.Equal(10, storedCodes.Count);
        foreach (var recoveryCode in enrollment.Login.RecoveryCodes!)
        {
            Assert.DoesNotContain(storedCodes, item => item.CodeHash == recoveryCode);
            Assert.Contains(storedCodes, item =>
                item.CodeHash == RecoveryCodeService.Hash(recoveryCode));
        }
    }

    [Fact]
    public async Task RecoveryCode_IsSingleUse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "recovery-admin", "Operations", cancellationToken);
        using var client = factory.CreateClient();
        var enrollment = await EnrollAsync(factory, client, admin.Username, cancellationToken);
        var recoveryCode = Assert.Single(enrollment.Login.RecoveryCodes!.Take(1));

        var firstChallenge = await LoginForChallengeAsync(
            client,
            admin.Username,
            "TwoFactorRequired",
            cancellationToken);
        var firstRecovery = await client.PostAsJsonAsync(
            "/api/auth/2fa/verify",
            new
            {
                challengeToken = firstChallenge,
                recoveryCode
            },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstRecovery.StatusCode);

        var secondChallenge = await LoginForChallengeAsync(
            client,
            admin.Username,
            "TwoFactorRequired",
            cancellationToken);
        var replay = await client.PostAsJsonAsync(
            "/api/auth/2fa/verify",
            new
            {
                challengeToken = secondChallenge,
                recoveryCode
            },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usedCode = await db.TwoFactorRecoveryCodes.SingleAsync(
            item => item.CodeHash == RecoveryCodeService.Hash(recoveryCode),
            cancellationToken);
        Assert.NotNull(usedCode.UsedAt);
    }

    [Fact]
    public async Task VerificationChallenge_LocksAfterConfiguredFailedAttempts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "locked-admin", "Auditor", cancellationToken);
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, admin.Username, cancellationToken);
        var loginResult = Assert.IsType<LoginResponse>(
            await login.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken));
        var setup = Assert.IsType<SetupResponse>(
            await (await client.PostAsJsonAsync(
                "/api/auth/2fa/setup",
                new { challengeToken = loginResult.ChallengeToken },
                cancellationToken)).Content.ReadFromJsonAsync<SetupResponse>(cancellationToken));

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failure = await client.PostAsJsonAsync(
                "/api/auth/2fa/verify",
                new { challengeToken = setup.ChallengeToken, code = "000000" },
                cancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }

        var validCode = ComputeCurrentCode(factory, setup.ManualKey);
        var locked = await client.PostAsJsonAsync(
            "/api/auth/2fa/verify",
            new { challengeToken = setup.ChallengeToken, code = validCode },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.UserSessions.ToListAsync(cancellationToken));
        Assert.Equal(
            5,
            (await db.TwoFactorChallenges.SingleAsync(cancellationToken)).FailedAttemptCount);
    }

    [Fact]
    public async Task StandardUser_LoginRemainsSingleStepWhenTwoFactorIsNotEnabled()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var user = await SeedUserAsync(factory, "standard-user", "User", cancellationToken);
        using var client = factory.CreateClient();

        var response = await LoginAsync(client, user.Username, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.IsType<LoginResponse>(
            await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken));
        Assert.Equal("Authenticated", result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Null(result.ChallengeToken);
    }

    [Fact]
    public async Task ProfileEnrollment_ReturnsServiceUnavailableWhenEncryptionKeyIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(twoFactorEncryptionKey: string.Empty);
        var admin = await SeedUserAsync(
            factory,
            "admin-without-2fa-key",
            "Admin",
            cancellationToken);
        using var client = factory.CreateAuthenticatedClient(admin.Id, admin.Role);

        var statusResponse = await client.GetAsync(
            "/api/profile/2fa",
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = Assert.IsType<TwoFactorStatusResponse>(
            await statusResponse.Content.ReadFromJsonAsync<TwoFactorStatusResponse>(
                cancellationToken));
        Assert.False(status.IsAvailable);

        var setupResponse = await client.PostAsJsonAsync(
            "/api/profile/2fa/setup",
            new { currentPassword = TestPassword },
            cancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, setupResponse.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(
            await setupResponse.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken));
        Assert.Equal("Dịch vụ tạm thời chưa sẵn sàng", problem.Title);
        Assert.Contains("encryption key", problem.Detail, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.TwoFactorChallenges.ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task UnverifiedPrivilegedSession_CannotAccessOrRefreshWhenEnforcementIsEnabled()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "legacy-admin-session", "Admin", cancellationToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trackedAdmin = await db.Users.SingleAsync(item => item.Id == admin.Id, cancellationToken);
        var sessionService = scope.ServiceProvider.GetRequiredService<AuthSessionService>();
        var issued = sessionService.CreateSession(trackedAdmin, DateTime.UtcNow, "pwd", null);
        await db.SaveChangesAsync(cancellationToken);
        var session = await db.UserSessions.SingleAsync(cancellationToken);

        var accessAllowed = await scope.ServiceProvider
            .GetRequiredService<IUserActivityService>()
            .ValidateAccessAndTrackAsync(
                admin.Id,
                admin.Role,
                session.TokenId,
                cancellationToken);
        var refreshed = await sessionService.RotateAsync(
            issued.RefreshToken,
            cancellationToken);

        Assert.False(accessAllowed);
        Assert.Null(refreshed);
        Assert.NotNull(session.RevokedAt);
        Assert.Equal("Two-factor verification is required", session.RevokeReason);
    }

    [Fact]
    public async Task PrivilegedAccount_CannotDisableMandatoryTwoFactor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "mandatory-admin", "MarketDataManager", cancellationToken);
        using var anonymousClient = factory.CreateClient();
        await EnrollAsync(factory, anonymousClient, admin.Username, cancellationToken);
        using var authenticatedClient = factory.CreateAuthenticatedClient(admin.Id, admin.Role);

        var response = await authenticatedClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/profile/2fa")
            {
                Content = JsonContent.Create(new
                {
                    currentPassword = TestPassword,
                    code = "000000"
                })
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PrivilegedAdminEndpoint_RejectsSessionWithoutMfaClaimWhenEnforced()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var admin = await SeedUserAsync(factory, "policy-admin", "Admin", cancellationToken);
        using var client = factory.CreateAuthenticatedClient(
            admin.Id,
            admin.Role,
            mfaVerified: false);

        var response = await client.GetAsync(
            "/api/admin/control-plane/capabilities",
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdminReset_ClearsTwoFactorArtifactsRevokesSessionsAndAudits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var superAdmin = await SeedUserAsync(
            factory,
            "recovery-superadmin",
            "SuperAdmin",
            cancellationToken);
        var target = await SeedUserAsync(
            factory,
            "locked-operator",
            "Operations",
            cancellationToken);

        using (var arrangeScope = factory.Services.CreateScope())
        {
            var db = arrangeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trackedTarget = await db.Users.SingleAsync(
                item => item.Id == target.Id,
                cancellationToken);
            var protector = arrangeScope.ServiceProvider
                .GetRequiredService<TwoFactorSecretProtector>();
            trackedTarget.TwoFactorEnabled = true;
            trackedTarget.TwoFactorEnabledAt = DateTime.UtcNow;
            trackedTarget.TwoFactorSecretEncrypted = protector.Protect(
                "JBSWY3DPEHPK3PXP",
                trackedTarget.Id);
            db.TwoFactorRecoveryCodes.Add(new TwoFactorRecoveryCode
            {
                UserId = trackedTarget.Id,
                CodeHash = RecoveryCodeService.Hash("AAAA-BBBB-CCCC-DDDD")
            });
            arrangeScope.ServiceProvider.GetRequiredService<TwoFactorChallengeService>()
                .Issue(
                    trackedTarget,
                    TwoFactorChallengePurpose.Login,
                    DateTime.UtcNow);
            arrangeScope.ServiceProvider.GetRequiredService<AuthSessionService>()
                .CreateSession(
                    trackedTarget,
                    DateTime.UtcNow,
                    "otp",
                    DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateAuthenticatedClient(
            superAdmin.Id,
            superAdmin.Role,
            mfaVerified: true);
        var response = await client.PostAsJsonAsync(
            $"/api/admin/control-plane/users/{target.Id}/two-factor/reset",
            new
            {
                confirmation = target.Username,
                reason = "Operator lost authenticator and recovery codes"
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertScope = factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var resetUser = await assertDb.Users.SingleAsync(
            item => item.Id == target.Id,
            cancellationToken);
        Assert.False(resetUser.TwoFactorEnabled);
        Assert.Null(resetUser.TwoFactorSecretEncrypted);
        Assert.Empty(await assertDb.TwoFactorRecoveryCodes
            .Where(item => item.UserId == target.Id)
            .ToListAsync(cancellationToken));
        Assert.Empty(await assertDb.TwoFactorChallenges
            .Where(item => item.UserId == target.Id)
            .ToListAsync(cancellationToken));
        Assert.All(
            await assertDb.UserSessions
                .Where(item => item.UserId == target.Id)
                .ToListAsync(cancellationToken),
            session => Assert.NotNull(session.RevokedAt));
        Assert.Contains(
            await assertDb.AuditEvents.ToListAsync(cancellationToken),
            item => item.Action == "UserTwoFactorReset" &&
                item.EntityId == target.Id.ToString());
    }

    [Fact]
    public async Task DirectResetCommand_RequiresMfaAtMediatRBoundary()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory(enforceTwoFactorForPrivilegedRoles: true);
        var superAdmin = await SeedUserAsync(
            factory,
            "direct-superadmin",
            "SuperAdmin",
            cancellationToken);
        var target = await SeedUserAsync(
            factory,
            "direct-reset-target",
            "Admin",
            cancellationToken);
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, superAdmin.Id.ToString()),
                new Claim(ClaimTypes.Role, superAdmin.Role),
                new Claim("amr", "pwd")
            ], "DirectTest"))
        };

        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ResetUserTwoFactorCommand(
                    target.Id,
                    target.Username,
                    "Direct dispatch without second factor"),
                cancellationToken));

        Assert.Contains("Two-factor", exception.Message);
    }

    [Fact]
    public async Task ChallengeCleanup_DeletesOnlyRowsPastRetention()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = await SeedUserAsync(
            factory,
            "cleanup-user",
            "User",
            cancellationToken);
        using (var arrangeScope = factory.Services.CreateScope())
        {
            var db = arrangeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TwoFactorChallenges.AddRange(
                new TwoFactorChallenge
                {
                    UserId = user.Id,
                    TokenHash = new string('a', 64),
                    Purpose = TwoFactorChallengePurpose.Login,
                    CreatedAt = DateTime.UtcNow.AddHours(-50),
                    ExpiresAt = DateTime.UtcNow.AddHours(-48),
                    MaxAttempts = 5
                },
                new TwoFactorChallenge
                {
                    UserId = user.Id,
                    TokenHash = new string('b', 64),
                    Purpose = TwoFactorChallengePurpose.Login,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-6),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                    MaxAttempts = 5
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        var cleanup = new TwoFactorChallengeCleanupService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TwoFactorOptions { ChallengeRetentionHours = 24 }),
            NullLogger<TwoFactorChallengeCleanupService>.Instance);
        var deleted = await cleanup.CleanupOnceAsync(cancellationToken);

        Assert.Equal(1, deleted);
        using var assertScope = factory.Services.CreateScope();
        Assert.Single(await assertScope.ServiceProvider
            .GetRequiredService<AppDbContext>()
            .TwoFactorChallenges
            .ToListAsync(cancellationToken));
    }

    private const string TestPassword = "StrongPassword123!";

    private static async Task<User> SeedUserAsync(
        CorePortfolioApiFactory factory,
        string username,
        string role,
        CancellationToken cancellationToken)
    {
        var user = TestData.CreateUser(username);
        user.Role = role;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string username,
        CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = TestPassword },
            cancellationToken);

    private static async Task<string> LoginForChallengeAsync(
        HttpClient client,
        string username,
        string expectedStatus,
        CancellationToken cancellationToken)
    {
        var response = await LoginAsync(client, username, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.IsType<LoginResponse>(
            await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken));
        Assert.Equal(expectedStatus, result.Status);
        return Assert.IsType<string>(result.ChallengeToken);
    }

    private static async Task<EnrollmentResult> EnrollAsync(
        CorePortfolioApiFactory factory,
        HttpClient client,
        string username,
        CancellationToken cancellationToken)
    {
        var challengeToken = await LoginForChallengeAsync(
            client,
            username,
            "TwoFactorSetupRequired",
            cancellationToken);
        var setupResponse = await client.PostAsJsonAsync(
            "/api/auth/2fa/setup",
            new { challengeToken },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        var setup = Assert.IsType<SetupResponse>(
            await setupResponse.Content.ReadFromJsonAsync<SetupResponse>(cancellationToken));
        var code = ComputeCurrentCode(factory, setup.ManualKey);
        var verifyResponse = await client.PostAsJsonAsync(
            "/api/auth/2fa/verify",
            new { challengeToken, code },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var verified = Assert.IsType<LoginResponse>(
            await verifyResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken));
        return new EnrollmentResult(setup, verified, verifyResponse);
    }

    private static string ComputeCurrentCode(
        CorePortfolioApiFactory factory,
        string secret)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TotpService>()
            .ComputeCode(secret, DateTime.UtcNow);
    }

    private sealed record EnrollmentResult(
        SetupResponse Setup,
        LoginResponse Login,
        HttpResponseMessage VerifyResponse);

    private sealed record SetupResponse(
        string ChallengeToken,
        string ProvisioningUri,
        string ManualKey,
        DateTime ExpiresAt);

    private sealed record LoginResponse(
        string Status,
        string? Token,
        DateTime? ExpiresAt,
        Guid UserId,
        string Username,
        string? DisplayName,
        string? Email,
        string Role,
        string? ChallengeToken,
        DateTime? ChallengeExpiresAt,
        IReadOnlyList<string>? RecoveryCodes);
}
