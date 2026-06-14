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
        }).AllowAnonymous();

        group.MapPost("/login", async (ISender sender, [FromBody] LoginCommand command) =>
        {
            var result = await sender.Send(command);
            if (result == null) return Results.Unauthorized();
            return Results.Ok(result);
        }).AllowAnonymous();
    }
}
