using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.API.IntegrationTests.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTest";
    public const string UserIdHeader = "X-Test-User-Id";
    public const string RoleHeader = "X-Test-Role";
    public const string MfaHeader = "X-Test-Mfa";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userId) ||
            !Guid.TryParse(userId, out var parsedUserId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = Request.Headers.TryGetValue(RoleHeader, out var roleHeader)
            ? roleHeader.ToString()
            : "User";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, parsedUserId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        if (!Request.Headers.TryGetValue(MfaHeader, out var mfaHeader) ||
            !string.Equals(mfaHeader, "false", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("amr", "pwd"));
            claims.Add(new Claim("amr", "otp"));
        }
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
