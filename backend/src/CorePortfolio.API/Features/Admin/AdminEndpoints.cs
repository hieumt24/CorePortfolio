using CorePortfolio.API.Features.Admin.Overview;
using CorePortfolio.API.Features.Admin.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin;

public record UpdateUserAccessRequest(string Role, bool IsActive);

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(AdminPermissionCatalog.AdminAccess);

        group.MapGet("/overview", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAdminOverviewQuery(), cancellationToken)))
            .RequireAuthorization(AdminPermissionCatalog.OperationsRead);

        group.MapGet("/operations", (ProductionOperationsState operationsState) =>
            Results.Ok(operationsState.GetSnapshot()))
            .RequireAuthorization(AdminPermissionCatalog.OperationsRead);

        group.MapGet("/audit-events", async (
            AppDbContext dbContext,
            [FromQuery] string? action,
            [FromQuery] Guid? actorUserId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = dbContext.AuditEvents.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(item => item.Action == action.Trim());
            if (actorUserId.HasValue)
                query = query.Where(item => item.ActorUserId == actorUserId.Value);
            if (from.HasValue)
                query = query.Where(item => item.OccurredAt >= from.Value.ToUniversalTime());
            if (to.HasValue)
                query = query.Where(item => item.OccurredAt <= to.Value.ToUniversalTime());

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(item => item.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new
                {
                    item.Id,
                    item.ActorUserId,
                    item.Action,
                    item.EntityType,
                    item.EntityId,
                    item.Outcome,
                    item.IpAddress,
                    item.CorrelationId,
                    item.MetadataJson,
                    item.OccurredAt
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items, total, page, pageSize });
        }).RequireAuthorization(AdminPermissionCatalog.AuditRead);

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
                new GetAdminUsersQuery(search, role, isActive, isOnline, page, pageSize), cancellationToken)))
            .RequireAuthorization(AdminPermissionCatalog.UsersRead);

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
        }).RequireAuthorization(AdminPermissionCatalog.UsersManage);
    }
}
