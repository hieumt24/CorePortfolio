using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public record CreateTransactionCommand(Guid PortfolioId, Guid AssetId, TransactionType Type, decimal Quantity, decimal Price, string? Currency, DateTime? Timestamp) : IRequest<Guid>;
