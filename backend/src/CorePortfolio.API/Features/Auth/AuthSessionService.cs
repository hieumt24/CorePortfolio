using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Auth.Login;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CorePortfolio.API.Features.Auth;

public sealed record AuthSessionResult(
    LoginResult Response,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed class AuthSessionService(
    AppDbContext dbContext,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor)
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthSessionResult CreateSession(User user, DateTime now)
    {
        var accessTokenId = Guid.NewGuid().ToString("N");
        var refreshToken = GenerateRefreshToken();
        var refreshExpiresAt = now.Add(RefreshTokenLifetime);
        var session = new UserSession
        {
            UserId = user.Id,
            TokenId = accessTokenId,
            IpAddress = ClientIpAddress.Resolve(httpContextAccessor.HttpContext),
            UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = refreshExpiresAt
        };
        var refreshTokenEntity = new SessionRefreshToken
        {
            UserSession = session,
            TokenHash = HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt
        };
        dbContext.UserSessions.Add(session);
        dbContext.SessionRefreshTokens.Add(refreshTokenEntity);

        return new AuthSessionResult(
            CreateLoginResult(user, accessTokenId, now),
            refreshToken,
            refreshExpiresAt);
    }

    public async Task<AuthSessionResult?> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        var now = DateTime.UtcNow;
        var tokenHash = HashRefreshToken(refreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var storedToken = await dbContext.SessionRefreshTokens
            .Include(item => item.UserSession)
            .ThenInclude(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null) return null;

        var session = storedToken.UserSession;
        if (storedToken.ConsumedAt.HasValue || storedToken.RevokedAt.HasValue)
        {
            storedToken.ReuseDetectedAt = now;
            RevokeSession(session, now, "Refresh token reuse detected");
            dbContext.AuditEvents.Add(new AuditEvent
            {
                ActorUserId = session.UserId,
                Action = "RefreshTokenReuseDetected",
                EntityType = "UserSession",
                EntityId = session.Id.ToString(),
                Outcome = "Blocked",
                IpAddress = ClientIpAddress.Resolve(httpContextAccessor.HttpContext),
                OccurredAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        if (storedToken.ExpiresAt <= now || session.ExpiresAt <= now ||
            session.RevokedAt.HasValue || !session.User.IsActive)
        {
            RevokeSession(session, now, "Refresh session expired or access changed");
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        storedToken.ConsumedAt = now;
        var nextRefreshToken = GenerateRefreshToken();
        var nextToken = new SessionRefreshToken
        {
            UserSessionId = session.Id,
            TokenHash = HashRefreshToken(nextRefreshToken),
            CreatedAt = now,
            ExpiresAt = session.ExpiresAt
        };
        storedToken.ReplacedByTokenId = nextToken.Id;
        session.TokenId = Guid.NewGuid().ToString("N");
        session.LastSeenAt = now;
        dbContext.SessionRefreshTokens.Add(nextToken);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = session.UserId,
            Action = "AccessTokenRefreshed",
            EntityType = "UserSession",
            EntityId = session.Id.ToString(),
            Outcome = "Succeeded",
            IpAddress = ClientIpAddress.Resolve(httpContextAccessor.HttpContext),
            OccurredAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AuthSessionResult(
            CreateLoginResult(session.User, session.TokenId, now),
            nextRefreshToken,
            session.ExpiresAt);
    }

    public async Task RevokeByRefreshTokenAsync(
        string? refreshToken,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await dbContext.SessionRefreshTokens
            .Include(item => item.UserSession)
            .ThenInclude(item => item.RefreshTokens)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null) return;
        RevokeSession(storedToken.UserSession, DateTime.UtcNow, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.UserSessions
            .Include(item => item.RefreshTokens)
            .Where(item => item.UserId == userId && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var session in sessions) RevokeSession(session, now, reason);
        return sessions.Count;
    }

    private LoginResult CreateLoginResult(User user, string accessTokenId, DateTime now)
    {
        var expiresAt = now.Add(AccessTokenLifetime);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, accessTokenId)
        };
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not found.");
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var handler = new JwtSecurityTokenHandler();
        return new LoginResult
        {
            Token = handler.WriteToken(handler.CreateToken(descriptor)),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role
        };
    }

    private static void RevokeSession(UserSession session, DateTime now, string reason)
    {
        session.RevokedAt ??= now;
        session.RevokeReason ??= reason;
        foreach (var refreshToken in session.RefreshTokens.Where(item => item.RevokedAt == null))
            refreshToken.RevokedAt = now;
    }

    private static string GenerateRefreshToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();
}
