using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, TransactionMutationResult>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TransactionLedgerService _ledgerService;

    public CreateTransactionHandler(AppDbContext dbContext, ICurrentUserService currentUserService, TransactionLedgerService ledgerService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _ledgerService = ledgerService;
    }

    public async Task<TransactionMutationResult> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId && p.UserId == userId, cancellationToken);
        if (!portfolioExists)
            throw new Exception("Portfolio not found");

        var asset = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId && a.PortfolioId == request.PortfolioId, cancellationToken);
        if (asset == null)
            throw new Exception("Asset not found in this portfolio");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = request.Price,
            Fee = request.Fee,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Date = request.Timestamp ?? DateTime.UtcNow
        };

        await _ledgerService.ValidateHoldingAsync(transaction, userId, cancellationToken);
        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.Transactions.Add(transaction);
        await _ledgerService.SyncLedgerEntryAsync(transaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        var ledger = await _dbContext.CashLedgerEntries.Include(e => e.CashAccount)
            .SingleAsync(e => e.TransactionId == transaction.Id, cancellationToken);
        return new TransactionMutationResult(transaction.Id, ledger.Amount, ledger.CashAccount.Currency);
    }
}
