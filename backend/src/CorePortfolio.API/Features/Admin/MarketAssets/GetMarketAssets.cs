using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

using CorePortfolio.API.Common.Models;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record MarketAssetDto(Guid Id, Guid CategoryId, string CategoryName, string Symbol, string Name, decimal CurrentPrice, DateTime LastUpdated);

public record GetMarketAssetsQuery(Guid? CategoryId, int Page = 1, int PageSize = 10) : IRequest<PaginatedResult<MarketAssetDto>>;

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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.Symbol)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MarketAssetDto(m.Id, m.CategoryId, m.Category!.Name, m.Symbol, m.Name, m.CurrentPrice, m.LastUpdated))
            .ToListAsync(cancellationToken);
            
        return new PaginatedResult<MarketAssetDto>(items, totalCount, request.Page, request.PageSize);
    }
}
