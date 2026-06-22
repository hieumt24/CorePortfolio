using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Watchlist;

public record WatchlistDto(Guid Id, Guid MarketAssetId, string Symbol, string Name, decimal CurrentPrice, decimal? TargetPrice, DateTime AddedAt, string AssetCategoryName);

public record GetWatchlistQuery : IRequest<List<WatchlistDto>>;

public class GetWatchlistHandler : IRequestHandler<GetWatchlistQuery, List<WatchlistDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetWatchlistHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<WatchlistDto>> Handle(GetWatchlistQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        return await _dbContext.WatchlistItems
            .Include(w => w.MarketAsset)
                .ThenInclude(m => m.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => new WatchlistDto(
                w.Id,
                w.MarketAssetId,
                w.MarketAsset!.Symbol,
                w.MarketAsset.Name,
                w.MarketAsset.CurrentPrice,
                w.TargetPrice,
                w.AddedAt,
                w.MarketAsset.Category!.Name
            ))
            .ToListAsync(cancellationToken);
    }
}
