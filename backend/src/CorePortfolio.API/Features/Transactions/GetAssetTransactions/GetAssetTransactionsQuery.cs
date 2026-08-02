using MediatR;

namespace CorePortfolio.API.Features.Transactions.GetAssetTransactions;

public record GetAssetTransactionsQuery(Guid AssetId) : IRequest<List<TransactionDto>>;

public record TransactionDto(
    Guid Id,
    int Type,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    string Notes,
    DateTime Timestamp,
    decimal? RemainingQuantity,
    decimal? UnrealizedPnl,
    bool? IsClosed
);
