using CorePortfolio.API.Services;
using MediatR;

namespace CorePortfolio.API.Features.Auth;

public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<AuthSessionResult?>;

public sealed class RefreshSessionHandler(AuthSessionService authSessionService)
    : IRequestHandler<RefreshSessionCommand, AuthSessionResult?>
{
    public Task<AuthSessionResult?> Handle(
        RefreshSessionCommand request,
        CancellationToken cancellationToken) =>
        authSessionService.RotateAsync(request.RefreshToken, cancellationToken);
}

public sealed record LogoutSessionCommand(string? RefreshToken) : IRequest;

public sealed class LogoutSessionHandler(AuthSessionService authSessionService)
    : IRequestHandler<LogoutSessionCommand>
{
    public Task Handle(LogoutSessionCommand request, CancellationToken cancellationToken) =>
        authSessionService.RevokeByRefreshTokenAsync(
            request.RefreshToken,
            "User logout",
            cancellationToken);
}

public sealed record LogoutAllSessionsCommand : IRequest<int>;

public sealed class LogoutAllSessionsHandler(
    AuthSessionService authSessionService,
    ICurrentUserService currentUserService,
    CorePortfolio.Infrastructure.Data.AppDbContext dbContext)
    : IRequestHandler<LogoutAllSessionsCommand, int>
{
    public async Task<int> Handle(
        LogoutAllSessionsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required.");
        var count = await authSessionService.RevokeAllForUserAsync(
            userId,
            "User logout from all devices",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return count;
    }
}

public static class RefreshTokenCookie
{
    public const string Name = "coreportfolio.refresh";

    public static void Write(
        HttpResponse response,
        string refreshToken,
        DateTime expiresAt,
        IWebHostEnvironment environment)
    {
        response.Cookies.Append(Name, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = environment.IsProduction(),
            SameSite = environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = new DateTimeOffset(expiresAt),
            IsEssential = true
        });
    }

    public static void Delete(HttpResponse response, IWebHostEnvironment environment)
    {
        response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = environment.IsProduction(),
            SameSite = environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/auth",
            IsEssential = true
        });
    }
}
