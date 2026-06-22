using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Watchlist;

public record AddToWatchlistCommand(Guid MarketAssetId, decimal? TargetPrice) : IRequest<Guid>;

public class AddToWatchlistHandler : IRequestHandler<AddToWatchlistCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AddToWatchlistHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(AddToWatchlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException();

        var exists = await _dbContext.WatchlistItems
            .AnyAsync(w => w.UserId == userId && w.MarketAssetId == request.MarketAssetId, cancellationToken);
            
        if (exists) throw new InvalidOperationException("Asset is already in watchlist");

        var item = new WatchlistItem
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            MarketAssetId = request.MarketAssetId,
            TargetPrice = request.TargetPrice,
            AddedAt = DateTime.UtcNow
        };

        _dbContext.WatchlistItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
