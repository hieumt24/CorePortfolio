using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Admin.ControlPlane;

public sealed record RevokeSessionsRequest(Guid? SessionId, string Reason);
public sealed record UpdateRoleRequest(string Role);
public sealed record BroadcastRequest(
    string Title, string Message, NotificationSeverity Severity, string? Role, string? Link, DateTime? ExpiresAt);
public sealed record RepairIntegrityRequest(string CheckKey, bool DryRun = true);
public sealed record UpdateConfigurationRequest(Dictionary<string, string> Settings);

public static class AdminControlPlaneEndpoints
{
    public static void MapAdminControlPlaneEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/control-plane").WithTags("Admin Control Plane").RequireAuthorization("Admin");

        group.MapGet("/capabilities", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetAdminCapabilitiesQuery(), ct)));
        group.MapGet("/audit-events", async (ISender sender, [AsParameters] AuditFilter filter, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetAuditEventsQuery(
                filter.Search, filter.Action, filter.EntityType, filter.Outcome, filter.ActorUserId,
                filter.IpAddress, filter.From, filter.To, filter.Page, filter.PageSize), ct)))
            .RequireAuthorization("Admin");
        group.MapGet("/users/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAdminUserDetailQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AdminUsersRead");
        group.MapGet("/users/{id:guid}/sessions", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetUserSessionsQuery(id), ct))).RequireAuthorization("AdminUsersRead");
        group.MapGet("/users/{id:guid}/security-timeline", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetSecurityTimelineQuery(id), ct))).RequireAuthorization("AdminUsersRead");
        group.MapPost("/users/{id:guid}/sessions/revoke", async (
            Guid id, [FromBody] RevokeSessionsRequest request, ISender sender, CancellationToken ct) =>
            Results.Ok(new { revoked = await sender.Send(new RevokeUserSessionsCommand(id, request.SessionId, request.Reason), ct) }))
            .RequireAuthorization("AdminUsersRead");
        group.MapPut("/users/{id:guid}/role", async (
            Guid id, [FromBody] UpdateRoleRequest request, ISender sender, CancellationToken ct) =>
            await sender.Send(new UpdateAdminRoleCommand(id, request.Role), ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("AdminRolesManage");
        group.MapGet("/market-data", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetMarketDataHealthQuery(), ct))).RequireAuthorization("AdminMarketData");
        group.MapPost("/jobs/{name}/run", async (string name, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RunAdminJobCommand(name), ct))).RequireAuthorization("AdminOperationsExecute");
        group.MapGet("/notification-campaigns", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetNotificationCampaignsQuery(), ct))).RequireAuthorization("AdminNotifications");
        group.MapPost("/notification-campaigns", async (
            [FromBody] BroadcastRequest request, ISender sender, CancellationToken ct) =>
            Results.Ok(new { recipients = await sender.Send(new BroadcastNotificationCommand(
                request.Title, request.Message, request.Severity, request.Role, request.Link, request.ExpiresAt), ct) }))
            .RequireAuthorization("AdminNotifications");
        group.MapGet("/data-integrity", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDataIntegrityQuery(), ct))).RequireAuthorization("AdminIntegrity");
        group.MapPost("/data-integrity/repair", async (
            [FromBody] RepairIntegrityRequest request, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RepairDataIntegrityCommand(request.CheckKey, request.DryRun), ct)))
            .RequireAuthorization("AdminIntegrity");
        group.MapGet("/configuration", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetAdminSystemConfigurationQuery(), ct))).RequireAuthorization("AdminRecovery");
        group.MapPut("/configuration", async (
            [FromBody] UpdateConfigurationRequest request, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateAdminSystemConfigurationCommand(request.Settings), ct)))
            .RequireAuthorization("AdminRolesManage");
    }
}

public sealed record AuditFilter(
    string? Search = null, string? Action = null, string? EntityType = null, string? Outcome = null,
    Guid? ActorUserId = null, string? IpAddress = null, DateTime? From = null, DateTime? To = null,
    int Page = 1, int PageSize = 50);
