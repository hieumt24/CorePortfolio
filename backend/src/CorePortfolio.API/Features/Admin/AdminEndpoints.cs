using CorePortfolio.API.Features.Admin.Overview;
using CorePortfolio.API.Features.Admin.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Admin;

public record UpdateUserAccessRequest(string Role, bool IsActive);

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("Admin");

        group.MapGet("/overview", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAdminOverviewQuery(), cancellationToken)));

        group.MapGet("/users", async (
            ISender sender,
            [FromQuery] string? search,
            [FromQuery] string? role,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isOnline,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetAdminUsersQuery(search, role, isActive, isOnline, page, pageSize), cancellationToken)));

        group.MapPut("/users/{id:guid}/access", async (
            Guid id,
            [FromBody] UpdateUserAccessRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await sender.Send(
                    new UpdateUserAccessCommand(id, request.Role, request.IsActive), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(new { message = exception.Message });
            }
        });
    }
}
