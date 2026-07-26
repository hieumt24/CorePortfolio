using CorePortfolio.API.Features.Auth.Login;
using CorePortfolio.API.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (ISender sender, [FromBody] RegisterCommand command) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        }).AllowAnonymous().RequireRateLimiting("auth-register");

        group.MapPost("/login", async (
            ISender sender,
            HttpContext context,
            IWebHostEnvironment environment,
            [FromBody] LoginCommand command) =>
        {
            var result = await sender.Send(command);
            if (result == null) return Results.Unauthorized();
            RefreshTokenCookie.Write(
                context.Response,
                result.RefreshToken,
                result.RefreshTokenExpiresAt,
                environment);
            return Results.Ok(result.Response);
        }).AllowAnonymous().RequireRateLimiting("auth-login");

        group.MapPost("/refresh", async (
            ISender sender,
            HttpContext context,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!HasTrustedClientHeader(context.Request)) return Results.Forbid();
            context.Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var refreshToken);
            var result = await sender.Send(
                new RefreshSessionCommand(refreshToken ?? string.Empty),
                cancellationToken);
            if (result is null)
            {
                RefreshTokenCookie.Delete(context.Response, environment);
                return Results.Unauthorized();
            }
            RefreshTokenCookie.Write(
                context.Response,
                result.RefreshToken,
                result.RefreshTokenExpiresAt,
                environment);
            return Results.Ok(result.Response);
        }).AllowAnonymous().RequireRateLimiting("auth-refresh");

        group.MapPost("/logout", async (
            ISender sender,
            HttpContext context,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!HasTrustedClientHeader(context.Request)) return Results.Forbid();
            context.Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var refreshToken);
            await sender.Send(new LogoutSessionCommand(refreshToken), cancellationToken);
            RefreshTokenCookie.Delete(context.Response, environment);
            return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting("auth-refresh");

        group.MapPost("/logout-all", async (
            ISender sender,
            HttpContext context,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!HasTrustedClientHeader(context.Request)) return Results.Forbid();
            var revoked = await sender.Send(new LogoutAllSessionsCommand(), cancellationToken);
            RefreshTokenCookie.Delete(context.Response, environment);
            return Results.Ok(new { revoked });
        }).RequireAuthorization();
    }

    private static bool HasTrustedClientHeader(HttpRequest request) =>
        string.Equals(
            request.Headers["X-Requested-With"].ToString(),
            "CorePortfolio",
            StringComparison.Ordinal);
}
