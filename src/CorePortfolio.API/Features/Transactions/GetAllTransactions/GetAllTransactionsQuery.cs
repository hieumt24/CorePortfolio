using CorePortfolio.Domain.Entities;
using MediatR;

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
    DateTime Date
);

public record PaginatedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public record GetAllTransactionsQuery(
    Guid? PortfolioId,
    Guid? AssetId,
    TransactionType? Type,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedResult<GlobalTransactionDto>>;
