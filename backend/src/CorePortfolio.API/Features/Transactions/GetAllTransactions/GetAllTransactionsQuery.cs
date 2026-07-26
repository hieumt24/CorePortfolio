using CorePortfolio.Domain.Entities;
using MediatR;
using CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

namespace CorePortfolio.API.Features.Transactions.GetAllTransactions;

public record GlobalTransactionDto(
    Guid Id,
    Guid PortfolioId,
    string PortfolioName,
    Guid AssetId,
    string Symbol,
    string AssetName,
    string CategoryName,
    string Currency,
    TransactionType Type,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    string Notes,
    DateTime Date
);

public sealed record TransactionFacetCounts(
    int All,
    int Crypto,
    int Stock,
    int Fund);

public sealed record TransactionPageDto(
    List<GlobalTransactionDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    TransactionFacetCounts Facets);

public record GetAllTransactionsQuery(
    Guid? PortfolioId,
    Guid? AssetId,
    TransactionType? Type,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Search,
    TransactionAssetGroup AssetGroup,
    decimal? MinAmount,
    decimal? MaxAmount,
    string SortBy,
    string SortDirection,
    int Page = 1,
    int PageSize = 20
) : IRequest<TransactionPageDto>;
