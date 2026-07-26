using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Assets.SearchAvailableMarketAssets;

public record SearchAvailableMarketAssetsQuery(
    Guid PortfolioId,
    string? Search,
    Guid? CategoryId,
    int Limit = 20) : IRequest<IReadOnlyList<AvailableMarketAssetDto>>;

public record AvailableMarketAssetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Currency,
    string Symbol,
    string Name,
    decimal CurrentPrice,
    string PriceSource,
    string PriceStatus);

public sealed class SearchAvailableMarketAssetsHandler
    : IRequestHandler<SearchAvailableMarketAssetsQuery, IReadOnlyList<AvailableMarketAssetDto>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SearchAvailableMarketAssetsHandler(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AvailableMarketAssetDto>> Handle(
        SearchAvailableMarketAssetsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var ownsPortfolio = await _db.Portfolios
            .AnyAsync(
                portfolio => portfolio.Id == request.PortfolioId && portfolio.UserId == userId,
                cancellationToken);
        if (!ownsPortfolio)
            throw new ResourceNotFoundException("Không tìm thấy portfolio.");

        var existingMarketAssetIds = _db.Assets
            .Where(asset => asset.PortfolioId == request.PortfolioId)
            .Select(asset => asset.MarketAssetId);
        var query = _db.MarketAssets
            .AsNoTracking()
            .Where(asset => !existingMarketAssetIds.Contains(asset.Id));

        if (request.CategoryId.HasValue)
            query = query.Where(asset => asset.CategoryId == request.CategoryId.Value);

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100)
                throw new RequestValidationException("Nội dung tìm kiếm không được vượt quá 100 ký tự.");
            query = query.Where(asset =>
                asset.Symbol.ToLower().Contains(search)
                || asset.Name.ToLower().Contains(search));
        }

        var limit = Math.Clamp(request.Limit, 5, 50);
        var ordered = string.IsNullOrWhiteSpace(search)
            ? query.OrderBy(asset => asset.Symbol)
            : query
                .OrderByDescending(asset => asset.Symbol.ToLower() == search)
                .ThenByDescending(asset => asset.Symbol.ToLower().StartsWith(search))
                .ThenBy(asset => asset.Symbol);

        return await ordered
            .ThenBy(asset => asset.Id)
            .Take(limit)
            .Select(asset => new AvailableMarketAssetDto(
                asset.Id,
                asset.CategoryId,
                asset.Category!.Name,
                asset.Category.DefaultCurrency,
                asset.Symbol,
                asset.Name,
                asset.CurrentPrice,
                asset.PriceSource,
                asset.PriceStatus))
            .ToListAsync(cancellationToken);
    }
}

public static class SearchAvailableMarketAssetsEndpoint
{
    public static void MapSearchAvailableMarketAssetsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/portfolios/{portfolioId:guid}/available-market-assets",
                async (
                    Guid portfolioId,
                    IMediator mediator,
                    string? search,
                    Guid? categoryId,
                    int limit = 20) =>
                    Results.Ok(await mediator.Send(new SearchAvailableMarketAssetsQuery(
                        portfolioId,
                        search,
                        categoryId,
                        limit))))
            .WithName("SearchAvailableMarketAssets")
            .WithTags("Assets")
            .RequireAuthorization();
    }
}
