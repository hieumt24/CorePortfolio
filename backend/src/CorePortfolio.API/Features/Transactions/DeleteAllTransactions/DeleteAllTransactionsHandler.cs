using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

public class DeleteAllTransactionsHandler
    : IRequestHandler<DeleteAllTransactionsCommand, DeleteAllTransactionsResult>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAllTransactionsHandler(
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteAllTransactionsResult> Handle(
        DeleteAllTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng hiện tại.");

        await using var dbTransaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var transactions = await _dbContext.Transactions
            .Where(transaction =>
                transaction.Portfolio != null &&
                transaction.Portfolio.UserId == userId)
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            await dbTransaction.CommitAsync(cancellationToken);
            return new DeleteAllTransactionsResult(0);
        }

        var cashLedgerEntries = await _dbContext.CashLedgerEntries
            .Where(entry =>
                entry.Transaction != null &&
                entry.Transaction.Portfolio != null &&
                entry.Transaction.Portfolio.UserId == userId)
            .ToListAsync(cancellationToken);

        var linkedCashflows = await _dbContext.CashflowRecords
            .Where(cashflow =>
                cashflow.UserId == userId &&
                cashflow.Transaction != null &&
                cashflow.Transaction.Portfolio != null &&
                cashflow.Transaction.Portfolio.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.CashLedgerEntries.RemoveRange(cashLedgerEntries);
        _dbContext.CashflowRecords.RemoveRange(linkedCashflows);
        _dbContext.Transactions.RemoveRange(transactions);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
        return new DeleteAllTransactionsResult(transactions.Count);
    }
}
