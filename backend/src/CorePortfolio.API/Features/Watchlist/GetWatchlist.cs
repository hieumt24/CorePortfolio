using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Watchlist;

public record WatchlistDto(
    Guid Id,
    Guid MarketAssetId,
    string Symbol,
    string Name,
    decimal CurrentPrice,
    decimal? TargetPrice,
    DateTime AddedAt,
    string AssetCategoryName,
    string Currency,
    DateTime PriceUpdatedAt,
    string PriceStatus);

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
                .ThenInclude(m => m!.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => new WatchlistDto(
                w.Id,
                w.MarketAssetId,
                w.MarketAsset != null ? w.MarketAsset.Symbol : "",
                w.MarketAsset != null ? w.MarketAsset.Name : "",
                w.MarketAsset != null ? w.MarketAsset.CurrentPrice : 0,
                w.TargetPrice,
                w.AddedAt,
                w.MarketAsset != null && w.MarketAsset.Category != null ? w.MarketAsset.Category.Name : "",
                w.MarketAsset != null && w.MarketAsset.Category != null ? w.MarketAsset.Category.DefaultCurrency : "USD",
                w.MarketAsset != null ? w.MarketAsset.LastUpdated : DateTime.MinValue,
                w.MarketAsset != null ? w.MarketAsset.PriceStatus : "Unknown"
            ))
            .ToListAsync(cancellationToken);
    }
}
