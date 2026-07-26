using CorePortfolio.API.Features.Transactions.DeleteAllTransactions;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Transactions.GetAllTransactions;

public sealed class GetAllTransactionsHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetAllTransactionsQuery, TransactionPageDto>
{
    public async Task<TransactionPageDto> Handle(
        GetAllTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.MinAmount is < 0 || request.MaxAmount is < 0)
            throw new ArgumentException("Khoảng giá trị giao dịch không được âm.");
        if (request.MinAmount.HasValue &&
            request.MaxAmount.HasValue &&
            request.MinAmount > request.MaxAmount)
            throw new ArgumentException("Giá trị tối thiểu không được lớn hơn giá trị tối đa.");

        var query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.Portfolio != null &&
                transaction.Portfolio.UserId == currentUserService.UserId);

        if (request.PortfolioId.HasValue)
            query = query.Where(transaction => transaction.PortfolioId == request.PortfolioId.Value);
        if (request.AssetId.HasValue)
            query = query.Where(transaction => transaction.AssetId == request.AssetId.Value);
        if (request.Type.HasValue)
            query = query.Where(transaction => transaction.Type == request.Type.Value);
        if (request.StartDate.HasValue)
            query = query.Where(transaction => transaction.Date >= request.StartDate.Value);
        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value;
            if (endDate.TimeOfDay == TimeSpan.Zero)
            {
                var exclusiveEnd = endDate.Date.AddDays(1);
                query = query.Where(transaction => transaction.Date < exclusiveEnd);
            }
            else
            {
                query = query.Where(transaction => transaction.Date <= endDate);
            }
        }

        var search = request.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(transaction =>
                (transaction.Asset != null &&
                 transaction.Asset.MarketAsset != null &&
                 (transaction.Asset.MarketAsset.Symbol.Contains(search) ||
                  transaction.Asset.MarketAsset.Name.Contains(search))) ||
                (transaction.Portfolio != null && transaction.Portfolio.Name.Contains(search)) ||
                transaction.Notes.Contains(search));
        }

        if (request.MinAmount.HasValue)
            query = query.Where(transaction =>
                transaction.Quantity * transaction.Price + transaction.Fee >= request.MinAmount.Value);
        if (request.MaxAmount.HasValue)
            query = query.Where(transaction =>
                transaction.Quantity * transaction.Price + transaction.Fee <= request.MaxAmount.Value);

        var categoryCounts = await query
            .GroupBy(transaction =>
                transaction.Asset != null &&
                transaction.Asset.MarketAsset != null &&
                transaction.Asset.MarketAsset.Category != null
                    ? transaction.Asset.MarketAsset.Category.Name
                    : string.Empty)
            .Select(group => new { CategoryName = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var facets = new TransactionFacetCounts(
            categoryCounts.Sum(item => item.Count),
            categoryCounts.Where(item => AssetCategoryClassifier.IsCrypto(item.CategoryName))
                .Sum(item => item.Count),
            categoryCounts.Where(item => AssetCategoryClassifier.IsStock(item.CategoryName))
                .Sum(item => item.Count),
            categoryCounts.Where(item => AssetCategoryClassifier.IsFund(item.CategoryName))
                .Sum(item => item.Count));

        if (request.AssetGroup != TransactionAssetGroup.All)
        {
            var categoryIds = await dbContext.AssetCategories
                .AsNoTracking()
                .Select(category => new { category.Id, category.Name })
                .ToListAsync(cancellationToken);
            var matchingCategoryIds = categoryIds
                .Where(category => MatchesGroup(category.Name, request.AssetGroup))
                .Select(category => category.Id)
                .ToArray();
            query = query.Where(transaction =>
                transaction.Asset != null &&
                transaction.Asset.MarketAsset != null &&
                matchingCategoryIds.Contains(transaction.Asset.MarketAsset.CategoryId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplyOrdering(query, request.SortBy, request.SortDirection);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new GlobalTransactionDto(
                transaction.Id,
                transaction.PortfolioId,
                transaction.Portfolio != null ? transaction.Portfolio.Name : "N/A",
                transaction.AssetId,
                transaction.Asset != null && transaction.Asset.MarketAsset != null
                    ? transaction.Asset.MarketAsset.Symbol
                    : "N/A",
                transaction.Asset != null && transaction.Asset.MarketAsset != null
                    ? transaction.Asset.MarketAsset.Name
                    : "N/A",
                transaction.Asset != null &&
                transaction.Asset.MarketAsset != null &&
                transaction.Asset.MarketAsset.Category != null
                    ? transaction.Asset.MarketAsset.Category.Name
                    : "N/A",
                transaction.Asset != null &&
                transaction.Asset.MarketAsset != null &&
                transaction.Asset.MarketAsset.Category != null
                    ? transaction.Asset.MarketAsset.Category.DefaultCurrency
                    : "USD",
                transaction.Type,
                transaction.Quantity,
                transaction.Price,
                transaction.Fee,
                transaction.Notes,
                transaction.Date))
            .ToListAsync(cancellationToken);

        return new TransactionPageDto(items, totalCount, page, pageSize, facets);
    }

    private static IQueryable<Transaction> ApplyOrdering(
        IQueryable<Transaction> query,
        string sortBy,
        string sortDirection)
    {
        var descending = !sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "amount" => descending
                ? query.OrderByDescending(item => item.Quantity * item.Price + item.Fee)
                    .ThenByDescending(item => item.Date)
                : query.OrderBy(item => item.Quantity * item.Price + item.Fee)
                    .ThenBy(item => item.Date),
            "quantity" => descending
                ? query.OrderByDescending(item => item.Quantity).ThenByDescending(item => item.Date)
                : query.OrderBy(item => item.Quantity).ThenBy(item => item.Date),
            "fee" => descending
                ? query.OrderByDescending(item => item.Fee).ThenByDescending(item => item.Date)
                : query.OrderBy(item => item.Fee).ThenBy(item => item.Date),
            "symbol" => descending
                ? query.OrderByDescending(item => item.Asset!.MarketAsset!.Symbol)
                    .ThenByDescending(item => item.Date)
                : query.OrderBy(item => item.Asset!.MarketAsset!.Symbol).ThenBy(item => item.Date),
            _ => descending
                ? query.OrderByDescending(item => item.Date).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.Date).ThenBy(item => item.Id)
        };
    }

    private static bool MatchesGroup(string? categoryName, TransactionAssetGroup assetGroup) =>
        assetGroup switch
        {
            TransactionAssetGroup.Crypto => AssetCategoryClassifier.IsCrypto(categoryName),
            TransactionAssetGroup.Stock => AssetCategoryClassifier.IsStock(categoryName),
            TransactionAssetGroup.Fund => AssetCategoryClassifier.IsFund(categoryName),
            _ => true
        };
}
