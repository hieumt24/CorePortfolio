using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Common;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Transactions.DeleteTransaction;

public class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TransactionLedgerService _ledgerService;

    public DeleteTransactionHandler(AppDbContext dbContext, ICurrentUserService currentUserService, TransactionLedgerService ledgerService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _ledgerService = ledgerService;
    }

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Asset)
                .ThenInclude(a => a!.Portfolio)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId &&
                t.Asset!.Portfolio!.UserId == _currentUserService.UserId, cancellationToken);

        if (transaction == null)
            throw new ResourceNotFoundException("Không tìm thấy giao dịch.");

        await _ledgerService.ValidateAfterDeleteAsync(transaction, cancellationToken);
        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            // If there's an associated CashflowRecord, delete it too to avoid orphaned cashflows
            var cashflow = await _dbContext.CashflowRecords
                .FirstOrDefaultAsync(c => c.TransactionId == request.TransactionId, cancellationToken);
            
            if (cashflow != null)
            {
                _dbContext.CashflowRecords.Remove(cashflow);
            }

            _dbContext.Transactions.Remove(transaction);
            await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
    }
}
