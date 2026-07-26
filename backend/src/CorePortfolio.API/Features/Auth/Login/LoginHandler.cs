using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CorePortfolio.API.Common;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CorePortfolio.Domain.Entities;

namespace CorePortfolio.API.Features.Auth.Login;

public class LoginCommand : IRequest<LoginResult?>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class LoginHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginHandler(
        AppDbContext dbContext,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
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
        var tokenId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = loginTime;
        user.LastActivityAt = loginTime;
        user.LastLoginIpAddress = ClientIpAddress.Resolve(_httpContextAccessor.HttpContext);
        _dbContext.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            TokenId = tokenId,
            IpAddress = user.LastLoginIpAddress,
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            CreatedAt = loginTime,
            LastSeenAt = loginTime,
            ExpiresAt = expiresAt
        });
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

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new LoginResult
        {
            Token = tokenHandler.WriteToken(token),
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role
        };
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
