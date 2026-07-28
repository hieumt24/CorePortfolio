using System.Text.Json;
using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;

namespace CorePortfolio.API.Features.Auth.Login;

public sealed class AuthLoginCompletionService(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    AuthSessionService authSessionService)
{
    public async Task<AuthSessionResult> CompleteAsync(
        User user,
        DateTime now,
        string authenticationMethod,
        DateTime? twoFactorVerifiedAt,
        CancellationToken cancellationToken)
    {
        user.LastLoginAt = now;
        user.LastActivityAt = now;
        user.LastLoginIpAddress = ClientIpAddress.Resolve(httpContextAccessor.HttpContext);
        var authSession = authSessionService.CreateSession(
            user,
            now,
            authenticationMethod,
            twoFactorVerifiedAt);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = user.Id,
            Action = "UserLoginSucceeded",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Outcome = "Succeeded",
            IpAddress = user.LastLoginIpAddress,
            MetadataJson = JsonSerializer.Serialize(new { authenticationMethod }),
            OccurredAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return authSession;
    }
}
