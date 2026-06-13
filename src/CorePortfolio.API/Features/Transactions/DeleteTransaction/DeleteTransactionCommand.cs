using MediatR;

namespace CorePortfolio.API.Features.Transactions.DeleteTransaction;

public record DeleteTransactionCommand(Guid TransactionId) : IRequest;
