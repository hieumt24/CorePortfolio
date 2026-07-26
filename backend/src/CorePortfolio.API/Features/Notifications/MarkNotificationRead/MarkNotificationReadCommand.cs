using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest;

public sealed class MarkNotificationReadHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var notification = await dbContext.Notifications.SingleOrDefaultAsync(
            item => item.Id == request.Id && item.UserId == userId,
            cancellationToken);
        if (notification is null)
            throw new ResourceNotFoundException("Không tìm thấy thông báo.");

        if (notification.ReadAt.HasValue)
            return;

        notification.ReadAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
