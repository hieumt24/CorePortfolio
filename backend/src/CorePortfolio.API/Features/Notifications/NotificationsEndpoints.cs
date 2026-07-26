using CorePortfolio.API.Features.Notifications.DismissNotification;
using CorePortfolio.API.Features.Notifications.GetNotifications;
using CorePortfolio.API.Features.Notifications.GetUnreadCount;
using CorePortfolio.API.Features.Notifications.MarkAllNotificationsRead;
using CorePortfolio.API.Features.Notifications.MarkNotificationRead;
using CorePortfolio.API.Features.Notifications.Preferences;
using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Notifications;

public static class NotificationsEndpoints
{
    public static void MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("", async (
            ISender sender,
            [FromQuery] bool unreadOnly = false,
            [FromQuery] NotificationType? type = null,
            [FromQuery] NotificationSeverity? severity = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default) =>
            Results.Ok(await sender.Send(
                new GetNotificationsQuery(unreadOnly, type, severity, page, pageSize),
                cancellationToken)));

        group.MapGet("/unread-count", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetUnreadCountQuery(), cancellationToken)));

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/read-all", async (ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(new DismissNotificationCommand(id), cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/preferences", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetNotificationPreferencesQuery(), cancellationToken)));

        group.MapPut("/preferences", async (
            [FromBody] UpdateNotificationPreferencesRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new UpdateNotificationPreferencesCommand(request.Preferences),
                cancellationToken)));
    }
}
