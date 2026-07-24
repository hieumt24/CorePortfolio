using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

using CorePortfolio.API.Common.Models;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record MarketAssetDto(Guid Id, Guid CategoryId, string CategoryName, string Symbol, string Name,
    decimal CurrentPrice, DateTime LastUpdated, string PriceSource, string? ExternalId,
    string PriceStatus, string? LastPriceError);

public record GetMarketAssetsQuery(
    Guid? CategoryId,
    string? Search = null,
    string? PriceSource = null,
    string? PriceStatus = null,
    string SortBy = "symbol",
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 10) : IRequest<PaginatedResult<MarketAssetDto>>;

public class GetMarketAssetsHandler : IRequestHandler<GetMarketAssetsQuery, PaginatedResult<MarketAssetDto>>
{
    private readonly AppDbContext _dbContext;
    public GetMarketAssetsHandler(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PaginatedResult<MarketAssetDto>> Handle(GetMarketAssetsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.MarketAssets.Include(m => m.Category).AsNoTracking();
        
        if (request.CategoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(m =>
                m.Symbol.ToLower().Contains(search) ||
                m.Name.ToLower().Contains(search) ||
                (m.ExternalId != null && m.ExternalId.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.PriceSource))
        {
            var priceSource = request.PriceSource.Trim().ToLower();
            query = query.Where(m => m.PriceSource.ToLower() == priceSource);
        }

        if (!string.IsNullOrWhiteSpace(request.PriceStatus))
        {
            var status = request.PriceStatus.Trim().ToLower();
            var staleCutoff = DateTime.UtcNow.AddHours(-48);
            query = status switch
            {
                "stale" => query.Where(m =>
                    m.PriceStatus.ToLower() == "stale" ||
                    (m.PriceStatus.ToLower() == "fresh" && m.LastUpdated < staleCutoff)),
                "fresh" => query.Where(m =>
                    m.PriceStatus.ToLower() == "fresh" && m.LastUpdated >= staleCutoff),
                _ => query.Where(m => m.PriceStatus.ToLower() == status)
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var descending = request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = request.SortBy.Trim().ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
            "category" => descending
                ? query.OrderByDescending(m => m.Category!.Name)
                : query.OrderBy(m => m.Category!.Name),
            "price" => descending
                ? query.OrderByDescending(m => m.CurrentPrice)
                : query.OrderBy(m => m.CurrentPrice),
            "updated" => descending
                ? query.OrderByDescending(m => m.LastUpdated)
                : query.OrderBy(m => m.LastUpdated),
            "source" => descending
                ? query.OrderByDescending(m => m.PriceSource)
                : query.OrderBy(m => m.PriceSource),
            "status" => descending
                ? query.OrderByDescending(m => m.PriceStatus)
                : query.OrderBy(m => m.PriceStatus),
            _ => descending ? query.OrderByDescending(m => m.Symbol) : query.OrderBy(m => m.Symbol)
        };

        var items = await orderedQuery
            .ThenBy(m => m.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MarketAssetDto(m.Id, m.CategoryId, m.Category!.Name, m.Symbol, m.Name,
                m.CurrentPrice, m.LastUpdated, m.PriceSource, m.ExternalId,
                m.PriceStatus == "Fresh" && m.LastUpdated < DateTime.UtcNow.AddHours(-48) ? "Stale" : m.PriceStatus,
                m.LastPriceError))
            .ToListAsync(cancellationToken);
            
        return new PaginatedResult<MarketAssetDto>(items, totalCount, request.Page, request.PageSize);
    }
}
