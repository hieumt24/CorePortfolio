using CorePortfolio.API.Common.Models;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(
    bool UnreadOnly = false,
    NotificationType? Type = null,
    NotificationSeverity? Severity = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<NotificationDto>>;

public sealed class GetNotificationsHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetNotificationsQuery, PaginatedResult<NotificationDto>>
{
    public async Task<PaginatedResult<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var now = DateTime.UtcNow;
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                notification.DismissedAt == null &&
                (notification.ExpiresAt == null || notification.ExpiresAt > now));

        if (request.UnreadOnly)
            query = query.Where(notification => notification.ReadAt == null);
        if (request.Type.HasValue)
            query = query.Where(notification => notification.Type == request.Type.Value);
        if (request.Severity.HasValue)
            query = query.Where(notification => notification.Severity == request.Severity.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<NotificationDto>(
            entities.Select(NotificationDto.FromEntity).ToList(),
            totalCount,
            page,
            pageSize);
    }
}
