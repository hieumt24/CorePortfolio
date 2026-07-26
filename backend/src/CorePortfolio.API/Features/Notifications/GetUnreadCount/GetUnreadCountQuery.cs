using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<UnreadNotificationCountDto>;

public sealed record UnreadNotificationCountDto(int Count);

public sealed class GetUnreadCountHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetUnreadCountQuery, UnreadNotificationCountDto>
{
    public async Task<UnreadNotificationCountDto> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var now = DateTime.UtcNow;
        var count = await dbContext.Notifications.CountAsync(
            notification =>
                notification.UserId == userId &&
                notification.ReadAt == null &&
                notification.DismissedAt == null &&
                (notification.ExpiresAt == null || notification.ExpiresAt > now),
            cancellationToken);
        return new UnreadNotificationCountDto(count);
    }
}
