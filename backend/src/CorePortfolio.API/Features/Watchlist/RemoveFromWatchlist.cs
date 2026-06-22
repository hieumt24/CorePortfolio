using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Watchlist;

public record RemoveFromWatchlistCommand(Guid Id) : IRequest;

public class RemoveFromWatchlistHandler : IRequestHandler<RemoveFromWatchlistCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFromWatchlistHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveFromWatchlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var item = await _dbContext.WatchlistItems
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.UserId == userId, cancellationToken);

        if (item == null) throw new KeyNotFoundException("Watchlist item not found");

        _dbContext.WatchlistItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
