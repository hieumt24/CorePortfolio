using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.DismissNotification;

public sealed record DismissNotificationCommand(Guid Id) : IRequest;

public sealed class DismissNotificationHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<DismissNotificationCommand>
{
    public async Task Handle(DismissNotificationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var notification = await dbContext.Notifications.SingleOrDefaultAsync(
            item => item.Id == request.Id && item.UserId == userId,
            cancellationToken);
        if (notification is null)
            throw new ResourceNotFoundException("Không tìm thấy thông báo.");

        if (notification.DismissedAt.HasValue)
            return;

        var now = DateTime.UtcNow;
        notification.DismissedAt = now;
        notification.ReadAt ??= now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
