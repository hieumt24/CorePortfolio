using MediatR;

namespace CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

public enum TransactionAssetGroup
{
    All = 0,
    Crypto = 1,
    Stock = 2,
    Fund = 3
}

public record DeleteAllTransactionsCommand(TransactionAssetGroup AssetGroup)
    : IRequest<DeleteAllTransactionsResult>;

public record DeleteAllTransactionsResult(int DeletedCount);
