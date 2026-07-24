using MediatR;

namespace CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

public record DeleteAllTransactionsCommand : IRequest<DeleteAllTransactionsResult>;

public record DeleteAllTransactionsResult(int DeletedCount);
