using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Transactions.UpdateTransaction;

public record UpdateTransactionCommand(Guid TransactionId, TransactionType Type, decimal Quantity, decimal Price, string? Currency, DateTime? Timestamp) : IRequest;
