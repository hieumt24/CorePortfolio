using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.API.Features.Auth.TwoFactor;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.Domain.Entities;

namespace CorePortfolio.API.Features.Auth.Login;

public class LoginCommand : IRequest<LoginFlowResult?>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    public string Status { get; set; } = "Authenticated";
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ChallengeToken { get; set; }
    public DateTime? ChallengeExpiresAt { get; set; }
    public IReadOnlyList<string>? RecoveryCodes { get; set; }
}

public sealed record LoginFlowResult(
    LoginResult Response,
    AuthSessionResult? Session);

public class LoginHandler : IRequestHandler<LoginCommand, LoginFlowResult?>
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthLoginCompletionService _loginCompletionService;
    private readonly TwoFactorPolicy _twoFactorPolicy;
    private readonly TwoFactorChallengeService _challengeService;

    public LoginHandler(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        AuthLoginCompletionService loginCompletionService,
        TwoFactorPolicy twoFactorPolicy,
        TwoFactorChallengeService challengeService)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _loginCompletionService = loginCompletionService;
        _twoFactorPolicy = twoFactorPolicy;
        _challengeService = challengeService;
    }

    public async Task<LoginFlowResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
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

        var now = DateTime.UtcNow;
        if (_twoFactorPolicy.RequiresTwoFactor(user))
        {
            var purpose = user.TwoFactorEnabled
                ? TwoFactorChallengePurpose.Login
                : TwoFactorChallengePurpose.Enrollment;
            var issued = _challengeService.Issue(user, purpose, now);
            _dbContext.AuditEvents.Add(new AuditEvent
            {
                ActorUserId = user.Id,
                Action = "TwoFactorChallengeIssued",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Outcome = "Pending",
                IpAddress = ClientIpAddress.Resolve(_httpContextAccessor.HttpContext),
                MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    purpose = purpose.ToString()
                }),
                OccurredAt = now
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new LoginFlowResult(
                new LoginResult
                {
                    Status = purpose == TwoFactorChallengePurpose.Enrollment
                        ? "TwoFactorSetupRequired"
                        : "TwoFactorRequired",
                    UserId = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Role = user.Role,
                    ChallengeToken = issued.Token,
                    ChallengeExpiresAt = issued.Challenge.ExpiresAt
                },
                null);
        }

        var authSession = await _loginCompletionService.CompleteAsync(
            user,
            now,
            "pwd",
            null,
            cancellationToken);
        return new LoginFlowResult(authSession.Response, authSession);
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
