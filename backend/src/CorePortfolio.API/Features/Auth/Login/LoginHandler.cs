using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.Domain.Entities;

namespace CorePortfolio.API.Features.Auth.Login;

public class LoginCommand : IRequest<AuthSessionResult?>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class LoginHandler : IRequestHandler<LoginCommand, AuthSessionResult?>
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthSessionService _authSessionService;

    public LoginHandler(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        AuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _authSessionService = authSessionService;
    }

    public async Task<AuthSessionResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);
        
        if (user == null)
        {
            await RecordFailedLoginAsync(null, request.Username, "InvalidCredentials", cancellationToken);
            return null;
        }

        if (!user.IsActive)
        {
            await RecordFailedLoginAsync(user, request.Username, "AccountDisabled", cancellationToken);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await RecordFailedLoginAsync(user, request.Username, "InvalidCredentials", cancellationToken);
            return null;
        }

        var loginTime = DateTime.UtcNow;
        user.LastLoginAt = loginTime;
        user.LastActivityAt = loginTime;
        user.LastLoginIpAddress = ClientIpAddress.Resolve(_httpContextAccessor.HttpContext);
        var authSession = _authSessionService.CreateSession(user, loginTime);
        _dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = user.Id,
            Action = "UserLoginSucceeded",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Outcome = "Succeeded",
            IpAddress = user.LastLoginIpAddress,
            OccurredAt = loginTime
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return authSession;
    }

    private async Task RecordFailedLoginAsync(
        User? user,
        string attemptedUsername,
        string reason,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = user?.Id,
            Action = "UserLoginFailed",
            EntityType = "User",
            EntityId = user?.Id.ToString(),
            Outcome = "Failed",
            IpAddress = ClientIpAddress.Resolve(_httpContextAccessor.HttpContext),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                username = attemptedUsername.Trim()[..Math.Min(attemptedUsername.Trim().Length, 50)],
                reason
            }),
            OccurredAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
