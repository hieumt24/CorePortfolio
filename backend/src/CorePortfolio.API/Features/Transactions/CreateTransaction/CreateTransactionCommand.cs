using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public record TransactionMutationResult(Guid Id, decimal CashImpact, string Currency);

public record CreateTransactionCommand(Guid PortfolioId, Guid? AssetId, TransactionType Type, decimal Quantity,
    decimal Price, string? Currency, DateTime? Timestamp, decimal Fee = 0, string? Notes = null,
    Guid? MarketAssetId = null)
    : IRequest<TransactionMutationResult>;
