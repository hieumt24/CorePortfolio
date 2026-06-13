using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record MarketAssetDto(Guid Id, Guid CategoryId, string CategoryName, string Symbol, string Name, decimal CurrentPrice, DateTime LastUpdated);

public record GetMarketAssetsQuery(Guid? CategoryId) : IRequest<List<MarketAssetDto>>;

public class GetMarketAssetsHandler : IRequestHandler<GetMarketAssetsQuery, List<MarketAssetDto>>
{
    private readonly AppDbContext _dbContext;
    public GetMarketAssetsHandler(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<MarketAssetDto>> Handle(GetMarketAssetsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.MarketAssets.Include(m => m.Category).AsNoTracking();
        
        if (request.CategoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == request.CategoryId.Value);
        }

        return await query
            .Select(m => new MarketAssetDto(m.Id, m.CategoryId, m.Category!.Name, m.Symbol, m.Name, m.CurrentPrice, m.LastUpdated))
            .ToListAsync(cancellationToken);
    }
}
