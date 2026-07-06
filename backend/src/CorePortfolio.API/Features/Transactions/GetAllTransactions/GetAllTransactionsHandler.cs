using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Common.Models;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Transactions.GetAllTransactions;

public class GetAllTransactionsHandler : IRequestHandler<GetAllTransactionsQuery, PaginatedResult<GlobalTransactionDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetAllTransactionsHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<GlobalTransactionDto>> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Transactions
            .Include(t => t.Portfolio)
            .Include(t => t.Asset)
                .ThenInclude(a => a.MarketAsset)
                    .ThenInclude(ma => ma.Category)
            .AsNoTracking()
            .Where(t => t.Portfolio != null && t.Portfolio.UserId == _currentUserService.UserId);

        // Apply filters
        if (request.PortfolioId.HasValue)
            query = query.Where(t => t.PortfolioId == request.PortfolioId.Value);

        if (request.AssetId.HasValue)
            query = query.Where(t => t.AssetId == request.AssetId.Value);

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        if (request.StartDate.HasValue)
            query = query.Where(t => t.Date >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(t => t.Date <= request.EndDate.Value);

        // Get total count for pagination
        int totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new GlobalTransactionDto(
                t.Id,
                t.PortfolioId,
                t.Portfolio != null ? t.Portfolio.Name : "N/A",
                t.AssetId,
                t.Asset != null && t.Asset.MarketAsset != null ? t.Asset.MarketAsset.Symbol : "N/A",
                t.Asset != null && t.Asset.MarketAsset != null ? t.Asset.MarketAsset.Name : "N/A",
                t.Asset != null && t.Asset.MarketAsset != null && t.Asset.MarketAsset.Category != null ? t.Asset.MarketAsset.Category.Name : "N/A",
                t.Asset != null && t.Asset.MarketAsset != null && t.Asset.MarketAsset.Category != null ? t.Asset.MarketAsset.Category.DefaultCurrency : "USD",
                t.Type,
                t.Quantity,
                t.Price,
                t.Fee,
                t.Notes,
                t.Date
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<GlobalTransactionDto>(items, totalCount, request.Page, request.PageSize);
    }
}
