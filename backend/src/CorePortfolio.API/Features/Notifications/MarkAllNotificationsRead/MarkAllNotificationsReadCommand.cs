using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand : IRequest<int>;

public sealed class MarkAllNotificationsReadHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var now = DateTime.UtcNow;
        var notifications = await dbContext.Notifications
            .Where(notification =>
                notification.UserId == userId &&
                notification.ReadAt == null &&
                notification.DismissedAt == null &&
                (notification.ExpiresAt == null || notification.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
            notification.ReadAt = now;

        if (notifications.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }
}
